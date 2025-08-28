using fairino;
using System;
using System.Threading;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using RMLib.Logger;
using RMLib.DataAccess;
using RMLib.PLC;
using RMLib.Alarms;
using System.Net.NetworkInformation;
using RM.src.RM250619.Forms.Plant;
using RM.Properties;
using RMLib.MessageBox;
using RM.src.RM250619.Classes.FR20.Applications.Application;
using RM.src.RM250619.Forms.DragMode;
using RM.src.RM250619.Classes.PLC;
using RM.src.RM250619.Classes.FR20.Jog;
using RM.src.RM250619.Classes.FR20;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Forms;
using RMLib.Security;

namespace RM.src.RM250619
{
    /// <summary>
    /// Gestisce il robot in tutte le sue funzioni, la classe contiene solo riferimenti statici poichè il robot è unico 
    /// nell'impianto. Nel caso se ne dovessero aggiungere dei nuovi bisognerà rifare la classe in modo che ci sia un array
    /// di Robot e i metodi per accedere alle funzioni di un singolo robot alla volta. 
    /// <br>Il robot restituisce come feedback per ogni metodo interno alla sua libreria un codice di errore che può essere
    /// controllato al fine di gestire la pagina degli allarmi.</br>
    /// <br>Il robot apparentemente si muove di pochi mm perciò non sta mai del tutto fermo, per fare il controllo sul movimento
    /// è necessario aggiungere degli offset.</br>
    /// <br>La libreria fairino presenta problemi a gestire la sincronizzazione tra comando ed esecuzione, per questo motivo 
    /// è difficile sapere a quale posizione il robot si sta muovendo. Inoltre sembra che a volte il robot non si fermi subito 
    /// al comando Stop, proprio per via della coda di istruzioni inviate.</br>
    /// </summary>
    public class RobotManager
    {
        #region Proprietà connessione al database

        private static readonly RobotDAOSqlite RobotDAO = new RobotDAOSqlite();
        private static readonly SqliteConnectionConfiguration DatabaseConnection = new SqliteConnectionConfiguration();
        private static readonly string ConnectionString = DatabaseConnection.GetConnectionString();

        #endregion

        #region Proprietà di RobotManager
        /// <summary>
        /// Errore che restituisce il Robot
        /// </summary>
        public static int err = 0;

        /// <summary>
        /// Codice principale errore Robot
        /// </summary>
        public static int maincode = 0;

        /// <summary>
        /// Codice specifico errore Robot
        /// </summary>
        public static int subcode = 0;

        /// <summary>
        /// Logger
        /// </summary>
        private static readonly log4net.ILog log = LogHelper.GetLogger();

        /// <summary>
        /// Istanza del Robot dalla libreria fairino
        /// </summary>
        public static Robot robot;

        /// <summary>
        /// IP statico assegnato al robot. Per modificarlo si deve usare il pannello dedicato.
        /// </summary>
        public static string RobotIpAddress = "192.168.2.70";

        /// <summary>
        /// Applicazione da far eseguire al Robot
        /// </summary>
        public static string applicationName;

        /// <summary>
        /// Indica se la modalità del robot è al momento in automatica o manuale se false
        /// </summary>
        public static bool isAutomaticMode;

        /// <summary>
        /// Percentuale di velocità
        /// </summary>
        public static int speed = 0;

        /// <summary>
        /// ID dello strumento in uso dal robot
        /// </summary>
        public static int tool = 0;

        /// <summary>
        /// Utente che sta usando il robot
        /// </summary>
        public static int user = 0;

        /// <summary>
        /// Carico massimo del robot in kg
        /// <br></br>
        /// Da 1 a FRX
        /// </summary>
        public static int weight = 0;

        /// <summary>
        /// Percentuale di speed
        /// </summary>
        public static float vel = 0;

        /// <summary>
        /// Percentuale di accelerazione
        /// </summary>
        public static float acc = 0;

        /// <summary>
        /// Fattore di scalatura di velocità
        /// </summary>
        public static float ovl = 0;

        /// <summary>
        /// Valore che rappresenta la smoothness dei movimenti del robot, un valore alto farà in modo da 
        /// "tagliare" tra un punto ed un altro, un valore basso invece sembrerà più "spigoloso"
        /// </summary>
        public static float blendT = 0;

        /// <summary>
        /// Configurazione dello spazio giunto
        /// </summary>
        public static int config = -1;

        /// <summary>
        /// Flag -> 0: blocking, 1: non_blocking
        /// </summary>
        public static byte flag = 0;

        /// <summary>
        /// Estensione area di lavoro Robot
        /// </summary>
        public static ExaxisPos ePos = new ExaxisPos(0, 0, 0, 0);

        /// <summary>
        /// Offset
        /// </summary>
        public static DescPose offset = new DescPose();

        /// <summary>
        /// True quando può partire la PlaceRoutine
        /// </summary>
        public static bool startPlaceRoutine = false;

        /// <summary>
        /// True quando può partire la HomeRoutine
        /// </summary>
        public static bool startHomeRoutine = false;

        /// <summary>
        /// True quando il ciclo puo essere avviato
        /// </summary>
        public static bool startCycle = false;

        /// <summary>
        /// Frequenza registrazione punti in DragMode
        /// </summary>
        public static int velRec = 500;

        /// <summary>
        /// Riferimento alla pagina degli allarmi
        /// </summary>
        public static FormAlarmPage formAlarmPage;

        /// <summary>
        /// Istanza form di diagnostica
        /// </summary>
        public static FormDiagnostics formDiagnostics;

        /// <summary>
        /// Posizione TCP attuale
        /// </summary>
        public static DescPose TCPCurrentPosition = new DescPose(0, 0, 0, 0, 0, 0);

        /// <summary>
        /// Evento per la generazione degli allarmi
        /// </summary>
        public static event EventHandler AllarmeGenerato;

        /// <summary>
        /// Evento per il reset degli allarmi
        /// </summary>
        public static event EventHandler AllarmeResettato;

        /// <summary>
        /// Specifica se il robot è abilitato
        /// </summary>
        public static bool robotEnable = true;

        /// <summary>
        /// Raccoglie le proprietà del robot in un oggetto
        /// </summary>
        public static RobotProperties robotProperties;

        private static PositionChecker checker_ingombro_pick;
        private static PositionChecker checker_ingombro_place;
        private static PositionChecker checker_ingombro_home;


        private static PositionChecker checker_pos;
        private static PositionChecker checker_safeZone;

        /// <summary>
        /// A true se robot in movimento
        /// </summary>
        private static bool isRobotMoving = false;

        /// <summary>
        /// A true quando robot pronto
        /// </summary>
        private static bool robotStarted = false;

        /// <summary>
        /// A true quando robot pronto
        /// </summary>
        private static bool robotOn = false;

        /// <summary>
        /// A true quando robot pronto
        /// </summary>
        private static bool robotReady = false;

        /// <summary>
        /// A true quando robot non pronto
        /// </summary>
        private static bool Connection_Robot_Error = false;

        /// <summary>
        /// Dizionario di allarmi
        /// </summary>
        private static Dictionary<string, bool> allarmiSegnalati = new Dictionary<string, bool>();

        /// <summary>
        /// Dichiarazione thread ad alta priorità
        /// </summary>
        private static Thread highPriorityThread;
        private static int highPriorityRefreshPeriod = 20;

        /// <summary>
        /// Dichiarazione thread ad alta priorità
        /// </summary>
        private static Thread testThread;
        private static int testRefreshPeriod = 100;

        /// <summary>
        /// Dichiarazione thread a priorità normale
        /// </summary>
        private static Thread normalPriorityThread;
        private static int normalPriorityRefreshPeriod = 50;
        private static bool stopNormalPriorityThread;
        public static bool normalPriorityThreadStarted = false;

        /// <summary>
        /// Indica l'indice del punto corrente nel ciclo
        /// </summary>
        public static int currentIndex = -1;

        /// <summary>
        /// A true quando viene richiesta una pausa del ciclo
        /// </summary>
        public static bool pauseCycleRequested = false;

        private static Thread auxiliaryThread;
        private static int auxiliaryThreadRefreshPeriod = 200;
        private static bool stopAuxiliaryThread;
        public static bool auxiliaryThreadStarted = false;

        private static Thread demoThread;
        private static int demoThreadRefreshPeriod = 50;
        private static bool stopDemoThread;
        public static bool demoThreadStarted = false;

        /// <summary>
        /// Indica la prenotazione di stop ciclo, prima di terminare il thread aspetta che sia stato fatto il place
        /// </summary>
        private static bool requestStopCycle = false;

        /// <summary>
        /// Dichiarazione thread a priorità bassa
        /// </summary>
        private static Thread lowPriorityThread;
        private static int lowPriorityRefreshPeriod = 300;

        private static Thread CommandsThrad;
        private static int hmiCommandsThradRefreshPeriod = 200;
        private static bool stopHmiCommandsThread = false;
        private static bool hmiCommandsThreadStarted = false;

        /// <summary>
        /// True quando le pinze del roboto sono state chiuse
        /// </summary>
        public static bool gripperClosed = false;

        /// <summary>
        /// True quando devo aprire le pinze
        /// </summary>
        public static bool closeGripper = false;

        public static bool inPickPosition = false;

        public static bool inPosition = false;

        public static DescPose endingPoint = new DescPose(0, 0, 0, 0, 0, 0);

        /// <summary>
        /// True quando le pinze del robot sono aperte
        /// </summary>
        public static bool gripperOpened = false;

        /// <summary>
        /// True quando le devo aprire le pinze
        /// </summary>
        public static bool openGripper = false;

        /// <summary>
        /// Stato precedente ingombro nastro
        /// </summary>
        private static bool prevRobotOutPick = true;

        /// <summary>
        /// Stato precedente ingombro teglia 1
        /// </summary>
        private static bool prevRobotOutPlace = true;

        /// <summary>
        /// Stato precedente ingombro teglia 2
        /// </summary>
        private static bool prevRobotOutWelding = true;

        /// <summary>
        /// Stato precedente ingombro home position
        /// </summary>
        private static bool? prevInHomePos = null;

        /// <summary>
        /// Stato precedente fuori ingombro
        /// </summary>
        private static bool prevFuoriIngombro = false;

        /// <summary>
        /// Offset sull'asse x per spostamento su teglie
        /// </summary>
        public static double xOffset = -150;

        /// <summary>
        /// Offset sull'asse y per spostamento su teglie
        /// </summary>
        public static double yOffset = 200;

        /// <summary>
        /// Numero di tentativi di ping al robot
        /// </summary>
        public static int numAttempsRobotPing = 0;

        /// <summary>
        /// Velocità di override del Robot
        /// </summary>
        public static int velocityOverride = 0;

        public static bool robotInPause = false;

        /// <summary>
        /// Riferimento allo step delle normal variables corrente
        /// </summary>
        public static int step = 0;

        /// <summary>
        /// A true quando bisogna fermare il ciclo di routine
        /// </summary>
        public static bool stopRoutine = false;

        /// <summary>
        /// A true quando viene richiesto lo stop del ciclo routine del robot
        /// </summary>
        public static bool robotCycleStopRequested = false;

        /// <summary>
        /// A true quando si trova in posizione di Pick
        /// </summary>
        public static bool isInPositionPick = false;

        /// <summary>
        /// A true quando si trova in posizione di Place
        /// </summary>
        public static bool isInPositionPlace = false;

        /// <summary>
        /// A true quando si trova in posizione di Welding
        /// </summary>
        public static bool isInPositionWelding = false;

        /// <summary>
        /// A true quando il robot si trova in safe zone
        /// </summary>
        public static bool isInSafeZone = false;

        /// <summary>
        /// A true quando il robot si trova in home zone
        /// </summary>
        public static bool isInHomeZone = false;

        /// <summary>
        /// A true quando il robot si trova in safe zone
        /// </summary>
        public static bool? prevIsInSafeZone = null;

        /// <summary>
        /// A true quando si trova in posizione di home
        /// </summary>
        public static bool isInPositionHome = false;

        /// <summary>
        /// Proprietà speed del Robot
        /// </summary>
        public static int speedRobot = 100;

        /// <summary>
        /// Lista di punti da visualizzare
        /// </summary>
        public static List<PointPosition> positionsToSend = new List<PointPosition>();

        /// <summary>
        /// Punto da aggiungure alla lista di posizioni
        /// </summary>
        public static PointPosition positionToSend = new PointPosition();

        /// <summary>
        /// Lista di punti da salvare
        /// </summary>
        public static List<PointPosition> positionsToSave = new List<PointPosition>();

        // Struttura thread
        private static TimerCallback timerCallback;
        private static System.Threading.Timer timer;


        /// <summary>
        /// Contatore spostamento catena
        /// </summary>
        public static int chainPos = 0;

        /// <summary>
        /// Evento invocato per disattivare/attivare il pulsante di go to home position in honme page
        /// </summary>
        public static event EventHandler EnableButtonHome;

        /// <summary>
        /// Evento invocato per disattivare/attivare i pulsanti di start e stop ciclo in home page
        /// </summary>
        public static event EventHandler EnableCycleButtons;

        /// <summary>
        /// Evento invocato al termine della routine per riabilitare i tasti per riavvio della routine
        /// </summary>
        public static event EventHandler EnableButtonCycleEvent;

        /// <summary>
        /// Evento invocato quanto dall'hmi viene rischiesto di registrare uno specifico punto
        /// </summary>
        public static event EventHandler<RobotPointRecordingEventArgs> RecordPoint;

        /// <summary>
        /// Evento invocato al termine della routine per riabilitare i tasti per riavvio della routine
        /// </summary>
        public static event EventHandler EnableDragModeButtons;

        /// <summary>
        /// Viene invocato quando si modifica la velocità del Robot
        /// </summary>
        public static event EventHandler RobotVelocityChanged;

        /// <summary>
        /// Viene invocato quando si modifica la modalità del Robot
        /// </summary>
        public static event EventHandler RobotModeChanged;

        /// <summary>
        /// Viene invocato quando si rileva che il robot si sta muovendo
        /// </summary>
        public static event EventHandler RobotIsMoving;

        /// <summary>
        /// A true quando viene terminata la routine del ciclo
        /// </summary>
        public static bool stopCycleRoutine = false;

        public static bool stopChainUpdaterThread = false;

        public static event EventHandler RobotPositionChanged;

        /// <summary>
        /// A true quando viene richiesta interruzione ciclo
        /// </summary>
        public static bool stopCycleRequested = false;

        #region Memorizzazione stati precedenti

        /// <summary>
        /// Variabile per memorizzare lo stato precedente di isInHomePosition
        /// </summary>
        private static bool? previousIsInHomePosition = null;

        /// <summary>
        /// Memorizza lo stato precedente della variabile open/close grippers dal PLC
        /// </summary>
        private static bool? previousGripperStatus = null;

        /// <summary>
        /// Memorizza lo stato precedente della variabile on/off barrier status dal PLC
        /// </summary>
        private static bool? previousBarrierStatus = false;

        /// <summary>
        /// Memorizza lo stato precedente della variabile start ciclo dal PLC
        /// </summary>
        private static bool? previousStartCommandStatus = null;

        /// <summary>
        /// Memorizza lo stato precedente della variabile stop ciclo dal PLC
        /// </summary>
        private static int? previousStopCommandStatus = null;

        /// <summary>
        /// Memorizza lo stato precedente della variabile go to home position dal PLC
        /// </summary>
        private static bool previousHomeCommandStatus = false;

        /// <summary>
        /// Memorizza lo stato della variabile selected pallet dal PLC
        /// </summary>
        private static int previousPalletCommandNumber = 0;

        /// <summary>
        /// Memorizza lo stato della variabile selected pallet dal PLC
        /// </summary>
        private static int previousFormatCommandNumber = 0;

        /// <summary>
        /// Memorizza lo stato della variabile selected box format dal PLC
        /// </summary>
        private static int previousBoxFormatCommandNumber = 0;

        private static int previousSelectedFormat = 0;

        /// <summary>
        /// Memorizza lo stato precedente della variabile on/off jog nastro dal PLC
        /// </summary>
        private static bool previousJogNastroCommandStatus = false;

        #endregion
        /// <summary>
        /// Riga della matrice di carico del pallet
        /// </summary>
        public static int riga = 0;
        /// <summary>
        /// Colonna della matrice di carico del pallet
        /// </summary>
        public static int colonna = 0;
        /// <summary>
        /// Strato della matrice di carico del pallet
        /// </summary>
        public static int strato = 0;

        /// <summary>
        /// Parametro larghezza della focaccia da HMI
        /// </summary>
        public static int larghezzaScatola = 300;
        /// <summary>
        /// Parametro profondità della focaccia da HMI
        /// </summary>
        public static int lunghezzaScatola = 300;
        /// <summary>
        /// Altezza del pallet da HMI
        /// </summary>
        public static int altezzaScatola = 100;

        /// <summary>
        /// Larghezza del pallet da HMI
        /// </summary>
        public static int larghezzaPallet = 800;
        /// <summary>
        ///  Lunghezza del pallet da HMI
        /// </summary>
        public static int lunghezzaPallet = 1200;
        /// <summary>
        /// Altezza del pallet da HMI
        /// </summary>
        public static int altezzaPallet = 100;

        /// <summary>
        /// Speed utilizzata in home routine
        /// </summary>
        private static int homeRoutineSpeed = 2;
        /// <summary>
        /// Velocity utilizzata in home routine
        /// </summary>
        private static int homeRoutineVel = 100;
        /// <summary>
        /// Acceleration utilizzata in home routine
        /// </summary>
        private static int homeRoutineAcc = 100;

        /// <summary>
        /// A true quando il robot viene messo in pausa
        /// </summary>
        public static bool robotIsPaused = false;

        /// <summary>
        /// Salva stato di override velocità precedente
        /// </summary>
        private static int previousVel = 0;

        /// <summary>
        /// Richiesta stop ciclo home
        /// </summary>
        static bool stopHomeRoutine = false;

        /// <summary>
        /// Step ciclo home
        /// </summary>
        static int stepHomeRoutine = 0;

        /// <summary>
        /// Stato precedente di robotReadyToStart
        /// </summary>
        private static bool prevRobotReadyToStart = false;

        /// <summary>
        /// Stato precedente di robotReadyToStart
        /// </summary>
        private static bool prevRobotHasProgramInMemory = false;

        /// <summary>
        /// A true quando robot in modalità automatica
        /// </summary>
        private static bool isAuto = false;

        /// <summary>
        /// Rappresenta il valore della modalità automatica nello step precedente
        /// </summary>
        private static bool prevIsAuto = false;

        /// <summary>
        /// A true quando robot in modalità manuale
        /// </summary>
        private static bool isManual = false;

        /// <summary>
        /// Rappresenta il valore della modalità manuale nello step precedente
        /// </summary>
        private static bool prevIsManual = false;

        private static bool prevIsOff = false;

        /// <summary>
        /// A true quando il robot si trova in home zone
        /// </summary>
        public static bool isInHomePosition = false;

        public static int mode = -1;

        /// <summary>
        /// Modalità precedente letta dal PLC
        /// </summary>
        private static int lastMode = -1;

        /// <summary>
        /// Timestamp dell'ultima modifica
        /// </summary>
        private static DateTime lastModeChangeTime;

        /// <summary>
        /// Modalità stabile da impostare
        /// </summary>
        private static int stableMode = -1;

        /// <summary>
        /// Valore di avvio ciclo main
        /// </summary>
        static int CycleRun_Main = 0;

        /// <summary>
        /// Valore di avvio ciclo pick
        /// </summary>
        static int CycleRun_Pick = 0;

        /// <summary>
        /// Valore di avvio ciclo place
        /// </summary>
        static int CycleRun_Place = 0;

        /// <summary>
        /// Valore di avvio ciclo home
        /// </summary>
        static int CycleRun_Home = 0;

        public static event EventHandler RobotInHomePosition;
        public static event EventHandler RobotNotInHomePosition;
        public static event EventHandler GripperStatusON;
        public static event EventHandler GripperStatusOFF;

        /// <summary>
        /// Stato precedente connessione plc
        /// </summary>
        private static bool prevIsPlcConnected = true;

        /// <summary>
        /// Segnale di stop della pick routine
        /// </summary>
        static bool stopPickRoutine = false;

        /// <summary>
        /// Step ciclo di pick
        /// </summary>
        static int stepPick = 0;

        /// <summary>
        /// Segnale di stop della place routine
        /// </summary>
        static bool stopPlaceRoutine = false;

        /// <summary>
        /// Step ciclo di place
        /// </summary>
        static int stepPlace = 0;

        /// <summary>
        /// Posizione precedente robot
        /// </summary>
        public static DescPose previousTCPposition = new DescPose(0, 0, 0, 0, 0, 0);

        /// <summary>
        /// Valore enable robot
        /// </summary>
        private static bool isEnable = false;

        /// <summary>
        /// Valore precedente enable robot
        /// </summary>
        private static bool prevIsEnable = false;

        /// <summary>
        /// Valore not enable robot
        /// </summary>
        private static bool isNotEnable = false;

        /// <summary>
        /// Valore precednete not enable robot
        /// </summary>
        private static bool prevIsNotEnable = false;

        /// <summary>
        /// Valore enable now robot
        /// </summary>
        public static bool isEnabledNow = false;

        /// <summary>
        /// A 1 quando il robot è in pausa
        /// </summary>
        public static int robotMove_inPause = 0;

        #endregion

        #region Metodi della classe RobotManager

        /// <summary>
        /// Invoca metodo relativo al cambio di velocità del robot
        /// </summary>
        /// <param name="vel">Velocità impostata al Robot</param>
        public static void TriggerRobotVelocityChangedEvent(int vel)
        {
            RobotVelocityChanged?.Invoke(vel, EventArgs.Empty);
        }

        /// <summary>
        /// Invoca metodo relativo al cambio di modalità del robot
        /// </summary>
        /// <param name="mode"></param>
        public static void TriggerRobotModeChangedEvent(int mode)
        {
            RobotModeChanged?.Invoke(mode, EventArgs.Empty);
        }

        /// <summary>
        /// Check su errori comunicati da PLC
        /// </summary>
        /// <param name="alarmValues"></param>
        /// <param name="alarmDescriptions"></param>
        /// <param name="now"></param>
        /// <param name="unixTimestamp"></param>
        /// <param name="dateTime"></param>
        /// <param name="formattedDate"></param>
        private static void GetPLCErrorCode(
            Dictionary<string, object> alarmValues,
            Dictionary<string, string> alarmDescriptions,
            DateTime now, 
            long unixTimestamp, 
            DateTime dateTime, 
            string formattedDate
            )
        {
            /*
            object alarmsPresent;

            lock (PLCConfig.appVariables)
            {
                alarmsPresent = PLCConfig.appVariables.getValue("PLC1_" + "Alarm present");

                if (Convert.ToBoolean(alarmsPresent))
                {
                    foreach (var key in alarmDescriptions.Keys)
                    {
                        alarmValues[key] = PLCConfig.appVariables.getValue("PLC1_" + key);
                    }
                }
            }
            */
            /*
            try
            {

                foreach (var key in alarmDescriptions.Keys)
                {
                    alarmValues[key] = PLCConfig.appVariables.getValue("PLC1_" + key);
                }

                // if (Convert.ToBoolean(alarmsPresent))
                // {
                now = DateTime.Now;
                unixTimestamp = ((DateTimeOffset)now).ToUnixTimeMilliseconds();
                dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).DateTime.ToLocalTime();
                formattedDate = dateTime.ToString("dd-MM-yyyy HH:mm:ss");

                foreach (var alarm in alarmValues)
                {
                    if (Convert.ToBoolean(alarm.Value) && !IsAlarmAlreadySignaled(alarm.Key))
                    {
                        string id = GenerateAlarmId(alarm.Key);
                        CreateRobotAlarm(id, alarmDescriptions[alarm.Key], formattedDate, "PLC", "ON");
                        MarkAlarmAsSignaled(alarm.Key);
                    }
                }
                // }
            }
            catch(Exception ex)
            {

            }
            */
        }

        /// <summary>
        /// Avvisa se un allarme è già stato segnalato
        /// </summary>
        /// <param name="alarmKey"></param>
        /// <returns></returns>
        private static bool IsAlarmAlreadySignaled(string alarmKey)
        {
            return allarmiSegnalati.ContainsKey(alarmKey) && allarmiSegnalati[alarmKey];
        }

        /// <summary>
        /// Imposta l'allarme come segnalato
        /// </summary>
        /// <param name="alarmKey"></param>
        private static void MarkAlarmAsSignaled(string alarmKey)
        {
            if (allarmiSegnalati.ContainsKey(alarmKey))
            {
                allarmiSegnalati[alarmKey] = true;
            }
            else
            {
                allarmiSegnalati.Add(alarmKey, true);
            }
        }

        public static int robotError = 0;

        /// <summary>
        /// Legge allarmi derivanti dal Robot
        /// </summary>
        /// <param name="updates"></param>
        /// <param name="allarmeSegnalato"></param>
        /// <param name="robotAlarm"></param>
        /// <param name="now"></param>
        /// <param name="id"></param>
        /// <param name="description"></param>
        /// <param name="timestamp"></param>
        /// <param name="device"></param>
        /// <param name="state"></param>
        /// <param name="unixTimestamp"></param>
        /// <param name="dateTime"></param>
        /// <param name="formattedDate"></param>
        private static void GetRobotErrorCode(
            List<(string key, bool value, string type)> updates, 
            bool allarmeSegnalato,
            DataRow robotAlarm, 
            DateTime now, 
            string id, 
            string description, 
            string timestamp, 
            string device,
            string state, 
            long unixTimestamp,
            DateTime dateTime, 
            string formattedDate
            )
        {
            if (AlarmManager.isRobotConnected)
            {
                err = RobotManager.robot.GetRobotErrorCode(ref maincode, ref subcode);
                if (maincode != 0 && !IsAlarmAlreadySignaled(maincode.ToString() + subcode.ToString()))
                {
                    robotAlarm = RobotDAO.GetRobotAlarm(ConnectionString, maincode, subcode);
                    if (robotAlarm != null)
                    {
                        Console.WriteLine($"mainErrCode {maincode} subErrCode {subcode} ");

                        // Ottieni la data e l'ora attuali
                        now = DateTime.Now;

                        // Calcola il timestamp Unix in millisecondi
                        unixTimestamp = ((DateTimeOffset)now).ToUnixTimeMilliseconds();

                        dateTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(unixTimestamp.ToString())).DateTime.ToLocalTime();
                        formattedDate = dateTime.ToString("dd-MM-yyyy HH:mm:ss");

                        if (robotAlarm["id"].ToString() == "")
                        {
                            id = "9999";
                            description = "Generic/Not found";
                            timestamp = formattedDate;
                            device = "Robot";
                            state = "ON";
                        }
                        else
                        {
                            id = robotAlarm["id"].ToString();
                            description = robotAlarm["descr_MainCode"].ToString() + ": " + robotAlarm["descr_SubCode"].ToString();
                            timestamp = formattedDate;
                            device = "Robot";
                            state = "ON";
                        }
                        CreateRobotAlarm(id, description, timestamp, device, state);
                        MarkAlarmAsSignaled(maincode.ToString() + subcode.ToString());
                        log.Warn(robotAlarm["descr_MainCode"].ToString() + ": " + robotAlarm["descr_SubCode"].ToString());

                        // Imposta la variabile di stato dell'allarme
                        allarmeSegnalato = true;
                    }
                    else
                    {
                        // Ottieni la data e l'ora attuali
                        now = DateTime.Now;

                        // Calcola il timestamp Unix in millisecondi
                        unixTimestamp = ((DateTimeOffset)now).ToUnixTimeMilliseconds();

                        dateTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(unixTimestamp.ToString())).DateTime.ToLocalTime();
                        formattedDate = dateTime.ToString("dd-MM-yyyy HH:mm:ss");

                        id = "9999";
                        description = "Generic/Not found";
                        timestamp = formattedDate;
                        device = "Robot";
                        state = "ON";

                        CreateRobotAlarm(id, description, timestamp, device, state);

                        // Imposta la variabile di stato dell'allarme
                        allarmeSegnalato = true;
                    }

                    // Segnalo che è presente un allarme bloccante (allarme robot)
                    AlarmManager.blockingAlarm = true;
                    robotError = 1;
                }
                else if (maincode == 0)
                {
                    // Reimposta la variabile di stato se l'allarme è risolto
                    allarmeSegnalato = false;
                    robotError = 0;
                }
            }

            Thread.Sleep(1000); // Attendere prima di controllare di nuovo

        }

        /// <summary>
        /// Metodo che inizializza Robot e lo accende
        /// </summary>
        /// <param name="robotIpAddress">Indirizzo IP Robot</param>
        /// <returns></returns>
        public static bool InitRobot(string robotIpAddress)
        {
            formAlarmPage = new FormAlarmPage();
            formAlarmPage.AlarmsCleared += RMLib_AlarmsCleared;

            formDiagnostics = new FormDiagnostics();

            // Istanzio il robot
            RobotIpAddress = robotIpAddress;
            robot = new Robot();
            robot.RPC(RobotIpAddress);
            robot.ResumeMotion();
            AlarmManager.isRobotConnected = true;


            if (err == -4)
            {
                log.Error("RPC exception durante Init del Robot");
                return false;
            }

            RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");
            // vel = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "speedRobot"));

            // Thread ad alta priorità (zone ingombro e inPosition)
            highPriorityThread = new Thread(new ThreadStart(CheckHighPriority));
            highPriorityThread.IsBackground = true;
            highPriorityThread.Priority = ThreadPriority.Highest;
            highPriorityThread.Start();

            // Thread a priorità normale (ausiliari robot)
            auxiliaryThread = new Thread(new ThreadStart(AuxiliaryWorker));
            auxiliaryThread.IsBackground = true;
            auxiliaryThread.Priority = ThreadPriority.Normal;
            auxiliaryThread.Start();

            // Thread a priorità bassa (allarmi)
            lowPriorityThread = new Thread(new ThreadStart(CheckLowPriority));
            lowPriorityThread.IsBackground = true;
            lowPriorityThread.Priority = ThreadPriority.Lowest;
            lowPriorityThread.Start();

            CommandsThrad = new Thread(new ThreadStart(CommandsChecker));
            CommandsThrad.IsBackground = true;
            CommandsThrad.Priority = ThreadPriority.Normal;
            CommandsThrad.Start();


            // InitRobotComponents();

            // Se fallisce setting della proprietà del Robot
            if (!SetRobotProperties())
                return false;

            robot.SetSpeed(robotProperties.Speed);

            ResetPLCVariables();

            return true;
        }

        /// <summary>
        /// Reset iniziali delle variabili PLC
        /// </summary>
        private static void ResetPLCVariables()
        {
            var HomePoint = ApplicationConfig.applicationsManager.GetPosition("1", "RM"); // Get punto di home point
            RefresherTask.AddUpdate(PLCTagName.HomePoint_X, HomePoint.x, "FLOAT"); // Scrittura xCoord punto di home point
            RefresherTask.AddUpdate(PLCTagName.HomePoint_Y, HomePoint.y, "FLOAT"); // Scrittura yCoord punto di home point
            RefresherTask.AddUpdate(PLCTagName.HomePoint_Z, HomePoint.z, "FLOAT"); // Scrittura zCoord punto di home point
            // RefresherTask.AddUpdate(PLCTagName.HomePoint_RX, HomePoint.rx, "FLOAT"); // Scrittura rxCoord punto di home point
            // RefresherTask.AddUpdate(PLCTagName.HomePoint_RY, HomePoint.ry, "FLOAT"); // Scrittura ryCoord punto di home point
            // RefresherTask.AddUpdate(PLCTagName.HomePoint_RZ, HomePoint.rz, "FLOAT"); // Scrittura rzCoord punto di home point

            var pickPoint_Box1 = ApplicationConfig.applicationsManager.GetPosition("101", "RM"); // Get punto di pick box 1
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box1_X, pickPoint_Box1.x, "FLOAT"); // Scrittura xCoord punto di pick box 1
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box1_Y, pickPoint_Box1.y, "FLOAT"); // Scrittura yCoord punto di pick box 1
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box1_Z, pickPoint_Box1.z, "FLOAT"); // Scrittura zCoord punto di pick box 1
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box1_RX, pickPoint_Box1.rx, "FLOAT"); // Scrittura rxCoord punto di pick box 1
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box1_RY, pickPoint_Box1.ry, "FLOAT"); // Scrittura ryCoord punto di pick box 1
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box1_RZ, pickPoint_Box1.rz, "FLOAT"); // Scrittura rzCoord punto di pick box 1

            var pickPoint_Box2 = ApplicationConfig.applicationsManager.GetPosition("201", "RM"); // Get punto di pick box 2
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box2_X, pickPoint_Box2.x, "FLOAT"); // Scrittura xCoord punto di pick box 2
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box2_Y, pickPoint_Box2.y, "FLOAT"); // Scrittura yCoord punto di pick box 2
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box2_Z, pickPoint_Box2.z, "FLOAT"); // Scrittura zCoord punto di pick box 2
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box2_RX, pickPoint_Box2.rx, "FLOAT"); // Scrittura rxCoord punto di pick box 2
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box2_RY, pickPoint_Box2.ry, "FLOAT"); // Scrittura ryCoord punto di pick box 2
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box2_RZ, pickPoint_Box2.rz, "FLOAT"); // Scrittura rzCoord punto di pick box 2

            var pickPoint_Box3 = ApplicationConfig.applicationsManager.GetPosition("301", "RM"); // Get punto di pick box 3
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box3_X, pickPoint_Box3.x, "FLOAT"); // Scrittura xCoord punto di pick box 3
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box3_Y, pickPoint_Box3.y, "FLOAT"); // Scrittura yCoord punto di pick box 3
            RefresherTask.AddUpdate(PLCTagName.PickPoint_Box3_Z, pickPoint_Box3.z, "FLOAT"); // Scrittura zCoord punto di pick box 3
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box3_RX, pickPoint_Box3.rx, "FLOAT"); // Scrittura rxCoord punto di pick box 3
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box3_RY, pickPoint_Box3.ry, "FLOAT"); // Scrittura ryCoord punto di pick box 3
            // RefresherTask.AddUpdate(PLCTagName.PickPoint_Box3_RZ, pickPoint_Box3.rz, "FLOAT"); // Scrittura rzCoord punto di pick box 3

            var PlacePoint_Pallet1_Box1 = ApplicationConfig.applicationsManager.GetPosition("1101", "RM"); // Get punto di place pallet 1 box 1
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box1_X, PlacePoint_Pallet1_Box1.x, "FLOAT"); // Scrittura xCoord punto di place pallet 1 box 1
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box1_Y, PlacePoint_Pallet1_Box1.y, "FLOAT"); // Scrittura yCoord punto di place pallet 1 box 1
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box1_Z, PlacePoint_Pallet1_Box1.z, "FLOAT"); // Scrittura zCoord punto di place pallet 1 box 1
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box1_RX, PlacePoint_Pallet1_Box1.rx, "FLOAT"); // Scrittura rxCoord punto di place pallet 1 box 1
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box1_RY, PlacePoint_Pallet1_Box1.ry, "FLOAT"); // Scrittura ryCoord punto di place pallet 1 box 1
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box1_RZ, PlacePoint_Pallet1_Box1.rz, "FLOAT"); // Scrittura rzCoord punto di place pallet 1 box 1

            var PlacePoint_Pallet1_Box2 = ApplicationConfig.applicationsManager.GetPosition("1201", "RM"); // Get punto di place pallet 1 box 2
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box2_X, PlacePoint_Pallet1_Box2.x, "FLOAT"); // Scrittura xCoord punto di place pallet 1 box 2
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box2_Y, PlacePoint_Pallet1_Box2.y, "FLOAT"); // Scrittura yCoord punto di place pallet 1 box 2
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box2_Z, PlacePoint_Pallet1_Box2.z, "FLOAT"); // Scrittura zCoord punto di place pallet 1 box 2
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box2_RX, PlacePoint_Pallet1_Box2.rx, "FLOAT"); // Scrittura rxCoord punto di place pallet 1 box 2
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box2_RY, PlacePoint_Pallet1_Box2.ry, "FLOAT"); // Scrittura ryCoord punto di place pallet 1 box 2
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box2_RZ, PlacePoint_Pallet1_Box2.rz, "FLOAT"); // Scrittura rzCoord punto di place pallet 1 box 2

            var PlacePoint_Pallet1_Box3 = ApplicationConfig.applicationsManager.GetPosition("1301", "RM"); // Get punto di place pallet 1 box 3
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box3_X, PlacePoint_Pallet1_Box3.x, "FLOAT"); // Scrittura xCoord punto di place pallet 1 box 3
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box3_Y, PlacePoint_Pallet1_Box3.y, "FLOAT"); // Scrittura yCoord punto di place pallet 1 box 3
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box3_Z, PlacePoint_Pallet1_Box3.z, "FLOAT"); // Scrittura zCoord punto di place pallet 1 box 3
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box3_RX, PlacePoint_Pallet1_Box3.rx, "FLOAT"); // Scrittura rxCoord punto di place pallet 1 box 3
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box3_RY, PlacePoint_Pallet1_Box3.ry, "FLOAT"); // Scrittura ryCoord punto di place pallet 1 box 3
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet1_Box3_RZ, PlacePoint_Pallet1_Box3.rz, "FLOAT"); // Scrittura rzCoord punto di place pallet 1 box 3

            var PlacePoint_Pallet2_Box1 = ApplicationConfig.applicationsManager.GetPosition("2101", "RM"); // Get punto di place pallet 2 box 1
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box1_X, PlacePoint_Pallet2_Box1.x, "FLOAT"); // Scrittura xCoord punto di place pallet 2 box 1
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box1_Y, PlacePoint_Pallet2_Box1.y, "FLOAT"); // Scrittura yCoord punto di place pallet 2 box 1
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box1_Z, PlacePoint_Pallet2_Box1.z, "FLOAT"); // Scrittura zCoord punto di place pallet 2 box 1
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box1_RX, PlacePoint_Pallet2_Box1.rx, "FLOAT"); // Scrittura rxCoord punto di place pallet 2 box 1
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box1_RY, PlacePoint_Pallet2_Box1.ry, "FLOAT"); // Scrittura ryCoord punto di place pallet 2 box 1
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box1_RZ, PlacePoint_Pallet2_Box1.rz, "FLOAT"); // Scrittura rzCoord punto di place pallet 2 box 1

            var PlacePoint_Pallet2_Box2 = ApplicationConfig.applicationsManager.GetPosition("2201", "RM"); // Get punto di place pallet 2 box 2
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box2_X, PlacePoint_Pallet2_Box2.x, "FLOAT"); // Scrittura xCoord punto di place pallet 2 box 2
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box2_Y, PlacePoint_Pallet2_Box2.y, "FLOAT"); // Scrittura yCoord punto di place pallet 2 box 2
            RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box2_Z, PlacePoint_Pallet2_Box2.z, "FLOAT"); // Scrittura zCoord punto di place pallet 2 box 2
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box2_RX, PlacePoint_Pallet2_Box2.rx, "FLOAT"); // Scrittura rxCoord punto di place pallet 2 box 2
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box2_RY, PlacePoint_Pallet2_Box2.ry, "FLOAT"); // Scrittura ryCoord punto di place pallet 2 box 2
            // RefresherTask.AddUpdate(PLCTagName.PlacePoint_Pallet2_Box2_RZ, PlacePoint_Pallet2_Box2.rz, "FLOAT"); // Scrittura rzCoord punto di place pallet 2 box 2

            RefresherTask.AddUpdate(PLCTagName.ACT_Step_MainCycle, 0, "INT16"); // Reset fase ciclo a PLC
            RefresherTask.AddUpdate(PLCTagName.ACT_Step_Cycle_Home, 0, "INT16"); // Reset fase ciclo a PLC
            RefresherTask.AddUpdate(PLCTagName.ACT_Step_Cycle_Pick, 0, "INT16"); // Reset fase ciclo a PLC
            RefresherTask.AddUpdate(PLCTagName.ACT_Step_Cycle_Place, 0, "INT16"); // Reset fase ciclo a PLC
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Main, 0, "INT16"); // Reset valore di avvio ciclo main 
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Home, 0, "INT16"); // Reset valore di avvio ciclo home 
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Pick, 0, "INT16"); // Reset valore di avvio ciclo pick 
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Place, 0, "INT16"); // Reset valore di avvio ciclo place 
        }
        
        /// <summary>
        /// Metodo che esegue check sui comandi derivanti dal plc
        /// </summary>
        private static void CommandsChecker()
        {
            //Aspetto che il valore cambi
            Thread.Sleep(2000); // Valutare se tenere il delay o far dei controlli se ha scritto  0 davvero

            while(true)
            {
                CheckCommandStart();
                CheckCommandStop();
                CheckCommandGoToHome();
                CheckCommandRecordPoint();
                CheckCommandResetAlarms();
                CheckVelCommand();

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Gestione comando di stop derivante da plc
        /// </summary>
        private static void CheckCommandStop()
        {
            // Get valore variabile di stop ciclo robot
            int stopStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_StopCicloAuto));

            if (stopStatus == 1 && previousStopCommandStatus != 1)
            {
                stopCycleRoutine = true; // Alzo segnale di stop ciclo main
                CycleRun_Main = 0; // Segnalo interruzione ciclo main
                step = 0; // reset step ciclo main

                stopHomeRoutine = true; // Alzo segnale di stop ciclo home
                CycleRun_Home = 0; // Segnalo interruzione ciclo home
                stepHomeRoutine = 0; // reset step ciclo home

                stopPickRoutine = true; // Alzo segnale di stop ciclo pick
                CycleRun_Pick = 0; // Segnalo interruzione ciclo pick
                stepPick = 0; // reset step ciclo pick

                stopPlaceRoutine = true; // Alzo segnale di stop ciclo place
                CycleRun_Place = 0; // Segnalo interruzione ciclo place
                stepPlace = 0; // reset step ciclo place

                robot.PauseMotion(); // Invio comando di pausa al robot
                Thread.Sleep(200); // Leggero ritardo per stabilizzare il robot
                robot.StopMotion(); // Stop Robot con conseguente cancellazione di coda di punti

                previousStopCommandStatus = 1;
            }
            else
            {
                previousStopCommandStatus = 0;
            }
        }

        /// <summary>
        /// Esegue check su cambio velocità derivante dal plc
        /// </summary>
        private static void CheckVelCommand()
        {
            // Get valore variabile di stop ciclo robot
            int velocity = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_OverrideAuto));

            // Check su cambio di stato
            if (velocity != previousVel)
            {
                RobotDAO.SetRobotVelocity(ConnectionString, Convert.ToInt16(velocity));
                RobotDAO.SetRobotAcceleration(ConnectionString, Convert.ToInt16(velocity));

                //Invoco metodo per cambiare etichetta velocità in homePage
                RobotVelocityChanged?.Invoke(velocity, EventArgs.Empty);

                // Aggiornamento della velocità precendete
                previousVel = velocity;
                
            }
        }

        /// <summary>
        /// Gestisce gli ausiliari del Robot
        /// </summary>
        private static void AuxiliaryWorker()
        {
            // Get del punto di home dal dizionario delle posizioni
            var pHome = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            // Creazione della DescPose del punto di Home
            DescPose homePose = new DescPose(pHome.x, pHome.y, pHome.z, pHome.rx, pHome.ry, pHome.rz);

            while (true)
            {
                CheckIsRobotEnable();
                CheckRobotMode();
               // CheckIsRobotInHomePosition(homePose);

                Thread.Sleep(auxiliaryThreadRefreshPeriod);
            }
        }

        /// <summary>
        /// Check su comando di start derivante da plc
        /// </summary>
        private static void CheckCommandStart()
        {
            // Get valore variabile di avvio ciclo robot
            int startStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_StartCicloAuto));

            // Get valore variabile di stop ciclo robot
            int stopStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_StopCicloAuto));

            // Check su cambio di stato
            if (Convert.ToBoolean(startStatus) != previousStartCommandStatus)
            {
                if(startStatus == 1 && stopStatus != 1) // Start
                {
                    // Controllo che il robot sia in automatico
                    if(!isAutomaticMode)
                    {
                        log.Warn("Tenativo di avvio ciclo con robot non in modalità automatica");
                        return;
                    }
                    // 1. Lancia la procedura per andare in home
                    // 2. Quando sono in home


                    if (larghezzaPallet == 0 || larghezzaScatola == 0) // aggiungi livello, scatola, rotazione // Controllo che sia stato selezionato il formato di pallet e scatola
                    {
                        log.Error("Nessun formato pallet e/o scatola selezionato");
                        return;
                    }

                    // Setto della velocità del Robot dalle sue proprietà memorizzate sul database
                    if (robotProperties.Speed > 1)
                    {
                        int speed = robotProperties.Speed;
                        robot.SetSpeed(speed);
                        log.Info($"Velocità Robot: {speed}");
                    }

                    if (!AlarmManager.isRobotMoving) // Se il Robot non è in movimento 
                    {
                        // separa pick, place e routine di home, sono indipendenti
                        MainCycle();
                        EnableButtonCycleEvent?.Invoke(0, EventArgs.Empty);
                        
                    }
                    else // Se il Robot è in movimento
                    {
                        log.Error("Impossibile inviare nuovi punti al Robot. Robot in movimento");
                    }
                }
                else // Stop
                {
                    stopCycleRequested = true;  // Valutare se alzare un bit o fermare subito il robot
                    EnableButtonCycleEvent?.Invoke(1, EventArgs.Empty);
                }

                previousStartCommandStatus = startStatus > 0;
            }
        }

        /// <summary>
        /// Check su comando di stop derivante da plc
        /// </summary>
        private async static Task CheckCommandGoToHome() // Questo metodo non deve bloccare il thread
        {
            int homeStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_GoHome));

            if (homeStatus == 1 && !previousHomeCommandStatus) // Go to home
            {
                previousHomeCommandStatus = true;
                HomeRoutine();
            }

            if (homeStatus == 0)
            {
                previousHomeCommandStatus = false; // reset status
            }
            
        }

        /// <summary>
        /// Check su accesso barriere
        /// </summary>
        private static async void CheckBarrierStatus()
        {
            int barrierStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.MovePause));

            ROBOT_STATE_PKG robot_state_pkg = new ROBOT_STATE_PKG();
            byte mov_robot_state = 0;

            // Controllo se c'è stato un cambio di stato nella barriera
            if (Convert.ToBoolean(barrierStatus) != previousBarrierStatus)
            {
                if (barrierStatus == 1)
                {
                    // Richiesta di pausa
                    robot.PauseMotion();
                    robotIsPaused = true;

                    // 🔁 Aspetta che lo stato robot diventi 3 (pausa)
                    const int maxAttempts = 3;
                    int attempt = 0;

                    do
                    {
                        robot.GetRobotRealTimeState(ref robot_state_pkg);
                        mov_robot_state = robot_state_pkg.robot_state;

                        if (mov_robot_state == 3)
                        {
                           // robotMove_inPause = 1;
                            break;
                        }
                        Thread.Sleep(100); // Attendi un po' prima di riprovare
                        attempt++;

                    } while (attempt < maxAttempts);

                    if (mov_robot_state != 3)
                    {
                        log.Error("ERRORE: Il robot non si è messo in pausa correttamente.");
                    }
                }
                else
                {
                    // Ripresa
                    await Task.Delay(1000);
                    robot.ResumeMotion();
                    robotIsPaused = false;
                   // robotMove_inPause = 0;
                }

                previousBarrierStatus = barrierStatus > 0;
            }
        }

        /// <summary>
        /// Check su comando di registrazione punto derivante da plc
        /// </summary>
        private static void CheckCommandRecordPoint()
        {
            // int recordPointCommand = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.SelectedPointRecordCommandIn));

            int recordPointCommand = 0;

            if (recordPointCommand > 0)
            {
                // Registrazione punto 

                DescPose newPoint = RecPoint();
                RecordPoint?.Invoke(null, new RobotPointRecordingEventArgs(recordPointCommand, newPoint));

                //Scrivo sul PLC i nuovi valori
                switch(recordPointCommand)
                {
                    // Punto 1
                    case 1:
                        break;
                    // Punto 2
                    case 2:
                        break;
                    // Punto 3
                    case 3:
                        break;
                    // Punto 4
                    case 4:
                        break;
                    // Altrimenti
                    default:
                        log.Warn($"Tentativo di sovrascrivere il punto: {recordPointCommand}, operazione annullata.");
                        break;
                }

                // Reset valore
             
            }
        }

        /// <summary>
        /// Check su comando di reset allarmi derivante da plc
        /// </summary>
        private static void CheckCommandResetAlarms()
        {
            int resetAlarmsCommand = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_ResetAlarms));

            if (resetAlarmsCommand > 0)
            {
                // Reset allarme
                RMLib_AlarmsCleared(null, EventArgs.Empty);

                // Reset valore
                RefresherTask.AddUpdate(PLCTagName.CMD_ResetAlarms, 0, "INT16");
            }
        }

        /// <summary>
        /// Verifica se il Robot si trova in posizione di Home
        /// </summary>
        private static void CheckIsRobotInHomePosition(DescPose homePose)
        {
            // Calcola lo stato corrente
            isInHomePosition = checker_pos.IsInPosition(homePose, TCPCurrentPosition);

            // Controlla se lo stato è cambiato rispetto al precedente
            if (previousIsInHomePosition == null || isInHomePosition != previousIsInHomePosition)
            {
                //if (stableMode == 2)
                {
                    if (isInHomePosition)
                    {
                        // Aggiorna l'icona della goto home pos in home page
                        RobotInHomePosition?.Invoke(null, EventArgs.Empty);
                    }
                    else
                    {
                        // Aggiorna l'icona della goto home pos in home page
                        RobotNotInHomePosition?.Invoke(null, EventArgs.Empty);
                    }
                }
                    
                // Aggiorna solo se c'è stato un cambiamento
                RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Home_inPos, isInHomePosition ? 1 : 0, "INT16");

                // Aggiorna lo stato precedente
                previousIsInHomePosition = isInHomePosition;
            }
        }
        

        /// <summary>
        /// Esegue check su modalità Robot
        /// </summary>
        private static void CheckRobotMode()
        {
            // Ottieni la modalità operativa dal PLC
            mode = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.Operating_Mode));

            // Controlla se la modalità è cambiata rispetto all'ultima lettura
            if (mode != lastMode)
            {
                // Aggiorna l'ultima modalità letta e il timestamp
                lastMode = mode;
                lastModeChangeTime = DateTime.Now;
                return; // Aspettiamo che il valore si stabilizzi
            }

            // Verifica se la modalità è rimasta invariata per almeno 1 secondo
            if (DateTime.Now - lastModeChangeTime >= TimeSpan.FromSeconds(2) && mode != stableMode)
            {
                // Modalità confermata stabile: aggiorniamo lo stato
                stableMode = mode;

                // Cambia la modalità del robot in base alla modalità stabile
                if (stableMode == 1 && !prevIsAuto) // Passaggio alla modalità automatica
                {
                    RobotManager.isAutomaticMode = true;
                    SetRobotMode(0);                  // Imposta il robot in modalità automatica
                    JogMovement.StopJogRobotThread(); // Ferma il thread di movimento manuale
                    prevIsAuto = true;
                    prevIsManual = false;
                    prevIsOff = false;
                    TriggerRobotModeChangedEvent(1);  // Evento: modalità automatica
                }
                else if (stableMode == 2 && !prevIsManual) // Passaggio alla modalità manuale
                {
                    RobotManager.isAutomaticMode = false;
                    SetRobotMode(1);                  // Imposta il robot in modalità manuale
                    prevIsManual = true;
                    prevIsAuto = false;
                    prevIsOff = false;
                    TriggerRobotModeChangedEvent(0);  // Evento: modalità manuale
                }
                else if (stableMode == 0 && !prevIsOff) // Passaggio alla modalità Off
                {
                    prevIsOff = true;
                    prevIsAuto = false;
                    prevIsManual = false;
                    TriggerRobotModeChangedEvent(3);  // Evento: modalità Off
                }
            }

            // Esegui logiche aggiuntive come il movimento manuale (Jog)
            if (isEnabledNow && stableMode == 2)
            {
                JogMovement.StartJogRobotThread(); // Avvia il thread di movimento manuale (Jog)
            }
        }

        /// <summary>
        /// Thread ad alta priorità che tiene monitorato movimento robot e zone di ingombro
        /// </summary>
        private static void CheckHighPriority()
        {
            // Lista di aggiornamenti da inviare al PLC
            List<(string key, bool value, string type)> updates = new List<(string, bool, string)>();

            
            #region ingombro

            // Zone di ingombro
            var pickPose = ApplicationConfig.applicationsManager.GetPosition("101","RM");
            var placePose = ApplicationConfig.applicationsManager.GetPosition("1101","RM");
            var homePose = ApplicationConfig.applicationsManager.GetPosition("1","RM");

            DescPose[] startPoints = new DescPose[]
            {
                new DescPose(pickPose.x, pickPose.y, pickPose.z, pickPose.rx, pickPose.ry, pickPose.rz),
                new DescPose(placePose.x, placePose.y, placePose.z, placePose.rx, placePose.ry, placePose.rz),
                new DescPose(homePose.x, homePose.y, homePose.z, homePose.rx, homePose.ry, homePose.rz),
            };

            // Oggetto che rileva ingombro pick
            double delta_ingombro_pick = 500.0;
            checker_ingombro_pick = new PositionChecker(delta_ingombro_pick);

            // Oggetto che rileva ingombro place
            double delta_ingombro_place = 500.0;
            checker_ingombro_place = new PositionChecker(delta_ingombro_place);

            // Oggetto che rileva ingombro home
            double delta_ingombro_home = 500.0;
            checker_ingombro_home = new PositionChecker(delta_ingombro_home);

            #endregion

            checker_pos = new PositionChecker(15.0);


            #region Safe zone

            // Dichiarazione del punto di safe
            var pSafeZone = ApplicationConfig.applicationsManager.GetPosition("pSafeZone", "RM");

            DescPose pointSafeZone = new DescPose(pSafeZone.x, pSafeZone.y, pSafeZone.z, pSafeZone.rx, pSafeZone.ry, pSafeZone.rz);

            // Oggetto che rileva safe zone
            double delta_safeZone = 300.0; // soglia
            checker_safeZone = new PositionChecker(delta_safeZone);

            #endregion

            checker_pos = new PositionChecker(5.0);

            while (true)
            {
                if (robot != null && AlarmManager.isRobotConnected)
                {
                    try
                    {
                        robot.GetActualTCPPose(flag, ref TCPCurrentPosition); // Leggo posizione robot TCP corrente
                        CheckIsRobotMoving(updates);
                        CheckIsRobotInObstructionArea(startPoints, updates);
                        CheckIsRobotInSafeZone(pointSafeZone);
                        CheckIsRobotInPos();
                        CheckBarrierStatus();
                        CheckStatusRobot();

                    }
                    catch (Exception e)
                    {
                        log.Error("RobotManager: errore durante la valutazione delle variabili HIGH: " + e.Message);
                    }
                }

                updates.Clear(); // Svuoto la lista di aggiornamento

                Thread.Sleep(highPriorityRefreshPeriod);
            }
        }

        public static int robotStatus = 0;
       
        private static void CheckStatusRobot()
        {
            ROBOT_STATE_PKG robot_state_pkg = new ROBOT_STATE_PKG();
            byte mov_robot_state = 0;

            robot.GetRobotRealTimeState(ref robot_state_pkg);
            mov_robot_state = robot_state_pkg.robot_state;
            robotStatus = mov_robot_state;
        }

        /// <summary>
        /// Esegue reset del contatore degli step delle routine
        /// </summary>
        public static void ResetRobotSteps()
        {
            step = 0;
        }

        /// <summary>
        /// Thread a priorità bassa che gestisce allarmi robot e PLC
        /// </summary>
        private static void CheckLowPriority()
        {
            // Lista di aggiornamenti da inviare al PLC
            List<(string key, bool value, string type)> updates = new List<(string, bool, string)>();

            DataRow robotAlarm = null;

            bool allarmeSegnalato = false;

            string id = "";
            string description = "";
            string timestamp = "";
            string device = "";
            string state = "";

            DateTime now = DateTime.Now;
            long unixTimestamp = ((DateTimeOffset)now).ToUnixTimeMilliseconds();
            DateTime dateTime = DateTime.Now;
            string formattedDate = dateTime.ToString("dd-MM-yyyy HH:mm:ss");

            Dictionary<string, object> alarmValues = new Dictionary<string, object>();
            Dictionary<string, string> alarmDescriptions = new Dictionary<string, string>
    {
        { "Safety NOK", "Ausiliari non pronti" },
        { "Modbus robot error", "Errore comunicazione modbus robot" },
        { "Robot Cycle Paused", "Ciclo robot in pausa" },
        { "Error plates full", "Teglie piene" },
        { "Check open gr.failed", "Controllo pinza aperta fallito" },
        { "Check pos_Dx gr. failed", "Controllo pinza chiusa fallito" },
        { "Robot fault present", "Errore robot" },
        { "US_Dx_Error", "Errore ultrasuono" },
        { "US_Dx_Enabled", "Ultrasuono abilitato" },
        { "US_Dx_Started", "Ultrasuono avviato" },
        { "US_Dx_Error_Disconnect", "Ultrasuono disconnesso" },
        { "Errore_Drive_Destro", "Mancata presa pinza robot" },
    };

            JointPos jPos = new JointPos(0, 0, 0, 0, 0, 0);
            JointPos j1_actual_pos = new JointPos(0, 0, 0, 0, 0, 0);
            JointPos j2_actual_pos = new JointPos(0, 0, 0, 0, 0, 0);
            JointPos j3_actual_pos = new JointPos(0, 0, 0, 0, 0, 0);
            JointPos j4_actual_pos = new JointPos(0, 0, 0, 0, 0, 0);
            JointPos j5_actual_pos = new JointPos(0, 0, 0, 0, 0, 0);
            JointPos j6_actual_pos = new JointPos(0, 0, 0, 0, 0, 0);

            while (true)
            {
                CheckPLCConnection();
                CheckRobotConnection(updates);
                GetRobotErrorCode(updates, allarmeSegnalato, robotAlarm, now, id, description, timestamp,
                    device, state, unixTimestamp, dateTime, formattedDate);

                CheckRobotPosition(jPos, j1_actual_pos, j2_actual_pos, j3_actual_pos, j4_actual_pos, j5_actual_pos, j6_actual_pos);

                GetPLCErrorCode(alarmValues, alarmDescriptions, now, unixTimestamp,
                    dateTime, formattedDate);

                CheckCurrentToolAndUser();

                SendUpdatesToPLC();

               

                Thread.Sleep(lowPriorityRefreshPeriod);
            }
        }
 
        /// <summary>
        /// Esegue scrittua su plc
        /// </summary>
        private static void SendUpdatesToPLC()
        {
            RefresherTask.AddUpdate(PLCTagName.ACT_Step_Cycle_Home, stepHomeRoutine, "INT16"); // Scrittura fase ciclo home a PLC
            RefresherTask.AddUpdate(PLCTagName.ACT_Step_MainCycle, step, "INT16"); // Scrittura fase ciclo main a PLC
            RefresherTask.AddUpdate(PLCTagName.ACT_Step_Cycle_Pick, stepPick, "INT16"); // Scrittura fase ciclo pick a PLC
            RefresherTask.AddUpdate(PLCTagName.ACT_Step_Cycle_Place, stepPlace, "INT16"); // Scrittura fase ciclo place a PLC
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Home, CycleRun_Home, "INT16"); // Scrittura valore avvio/stop ciclo home
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Main, CycleRun_Main, "INT16"); // Scrittura valore avvio/stop ciclo main
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Pick, CycleRun_Pick, "INT16"); // Scrittura valore avvio/stop ciclo pick
            RefresherTask.AddUpdate(PLCTagName.CycleRun_Place, CycleRun_Place, "INT16"); // Scrittura valore avvio/stop ciclo place
            // RefresherTask.AddUpdate(PLCTagName.Move_InPause, robotMove_inPause, "INT16"); // Scrittura feedback pausa del robot
            RefresherTask.AddUpdate(PLCTagName.Robot_error, robotError, "INT16"); // Scrittura stato errore del robot
            RefresherTask.AddUpdate(PLCTagName.Robot_enable, robotEnableStatus, "INT16"); // Scrittura stato enable del robot
            RefresherTask.AddUpdate(PLCTagName.Robot_status, robotStatus, "INT16"); // Scrittura stato del robot
            RefresherTask.AddUpdate(PLCTagName.ACT_N_Tool, currentTool, "INT16"); // Scrittura stato del robot
            RefresherTask.AddUpdate(PLCTagName.ACT_N_Frame, currentUser, "INT16"); // Scrittura stato del robot
            RefresherTask.AddUpdate(PLCTagName.ACT_CollisionLevel, currentCollisionLevel, "INT16"); // Scrittura stato del robot

        }

        public static int currentTool = 0;
        public static int currentUser = 0;
        public static int currentCollisionLevel = 0;

        private static void CheckCurrentToolAndUser()
        {
            robot.GetActualTCPNum(1, ref currentTool);
            robot.GetActualWObjNum(1, ref currentUser);
        }
        
        /// <summary>
        /// Check su connessione PLC
        /// </summary>
        private static void CheckPLCConnection()
        {
            if (!AlarmManager.isPlcConnected) // Se il PLC è disconnesso
            {
                string id = "0";
                string description = "PLC disconnesso. Il ciclo è stato terminato.";

                DateTime now = DateTime.Now;
                long unixTimestamp = ((DateTimeOffset)now).ToUnixTimeMilliseconds();
                DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(unixTimestamp.ToString())).DateTime.ToLocalTime();
                string formattedDate = dateTime.ToString("dd-MM-yyyy HH:mm:ss");

                string device = "PLC";
                string state = "ON";

                if (!IsAlarmAlreadySignaled(id))
                {
                    CreateRobotAlarm(id, description, formattedDate, device, state);
                    MarkAlarmAsSignaled(id);
                }

                prevIsPlcConnected = false;
            }
            else
            {
                if (!prevIsPlcConnected)
                {

                    RobotManager.stopRoutine = true;
                    RobotManager.normalPriorityThreadStarted = false;
                    robotCycleStopRequested = false;

                    RobotManager.ClearRobotAlarm();
                    RobotManager.ClearRobotQueue();
                    RobotManager.ResetRobotSteps();

                    prevIsPlcConnected = true;
                }
            }
        }

        /// <summary>
        /// Verifica se il punto corrente è all'interno dell'area di ingombro rispetto a uno qualsiasi dei punti di partenza
        /// </summary>
        /// <param name="startPoints">Array con i punti di partenza per Pick, Place e Home</param>
        /// <param name="updates">Lista di aggiornamenti</param>
        private static void CheckIsRobotInObstructionArea(DescPose[] startPoints, List<(string key, bool value, string type)> updates)
        {
            isInPositionPick = checker_ingombro_pick.IsInObstruction(startPoints[0], TCPCurrentPosition);
            isInPositionPlace = checker_ingombro_place.IsInObstruction(startPoints[1], TCPCurrentPosition);
            isInPositionHome = checker_ingombro_home.IsInObstruction(startPoints[2], TCPCurrentPosition);

            if (isInPositionPick)
            {
                if (prevRobotOutPick != false)
                {
                    prevRobotOutPick = false;
                    prevRobotOutPlace = true;
                    prevInHomePos = true;
                    prevFuoriIngombro = false;

                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Pick_inPos, 1, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Place_inPos, 0, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Home_inPos, 0, "INT16");
                }
            }
            else if (isInPositionPlace)
            {
                if (prevRobotOutPlace != false)
                {
                    prevRobotOutPlace = false;
                    prevRobotOutPick = true;
                    prevInHomePos = true;
                    prevFuoriIngombro = false;

                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Pick_inPos, 0, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Place_inPos, 1, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Home_inPos, 0, "INT16");
                }
            }
            else if (isInPositionHome)
            {
                if (prevInHomePos != false)
                {
                    prevInHomePos = false;
                    prevRobotOutPick = true;
                    prevRobotOutPlace = true;
                    prevFuoriIngombro = false;

                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Pick_inPos, 0, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Place_inPos, 0, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Home_inPos, 1, "INT16");
                }
            }
            else
            {
                if (prevFuoriIngombro != true)
                {
                    prevFuoriIngombro = true;
                    prevRobotOutPick = true;
                    prevRobotOutPlace = true;
                    prevInHomePos = true;

                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Pick_inPos, 0, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Place_inPos, 0, "INT16");
                    RefresherTask.AddUpdate(PLCTagName.ACT_Zone_Home_inPos, 0, "INT16");
                }
            }
        }

        /// <summary>
        /// Verifica se il punto corrente è all'interno dell'area di safe zone
        /// </summary>
        private static void CheckIsRobotInSafeZone(DescPose pSafeZone)
        {
            isInSafeZone = checker_safeZone.IsYLessThan(pSafeZone, TCPCurrentPosition);

            if (!AlarmManager.isFormReady)
                return;

            if (!isInSafeZone && prevIsInSafeZone != false) // Se il robot non è nella safe zone
            {
                prevIsInSafeZone = false;
                FormHomePage.Instance.RobotSafeZone.BackgroundImage = Resources.safeZone_yellow32;
              
            }
            else if (isInSafeZone && prevIsInSafeZone != true) // Se il robot è nella safe zone
            {
                prevIsInSafeZone = true;
                FormHomePage.Instance.RobotSafeZone.BackgroundImage = Resources.safeZone_green32;
            
            }

        }

        /// <summary>
        /// Verifica se il punto corrente corrisponde ai punti di pick e/o place
        /// </summary>
        private static void CheckIsRobotInPos()
        {
            bool isInPosition = checker_pos.IsInPosition(endingPoint, TCPCurrentPosition);

            if (isInPosition)
            {
                inPosition = true;
            }
            else
            {
                inPosition = false;
            }

        }

        /// <summary>
        /// Gestore dell'evento allarmi cancellati presente nella libreria RMLib.Alarms
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void RMLib_AlarmsCleared(object sender, EventArgs e)
        {
            var criteria = new List<(string device, string description)>
            {
                ("Robot", ""),
                ("", "PLC disconnesso. Il ciclo è stato terminato.")
            };

            bool isBlocking = formAlarmPage.IsBlockingAlarmPresent(criteria);

            if (isBlocking)
            {

                ClearRobotAlarm();
                ClearRobotQueue();

                // Segnalo che non ci sono più allarmi bloccanti
                AlarmManager.blockingAlarm = false;

                // Abilito il tasto Start per avviare nuovamente la routine
                EnableButtonCycleEvent?.Invoke(1, EventArgs.Empty);

                // Abilito i tasti relativi al monitoring
                EnableDragModeButtons?.Invoke(null, EventArgs.Empty);


            } 

            TriggerAllarmeResettato();

    

            // Reset degli allarmi segnalati
            foreach (var key in allarmiSegnalati.Keys.ToList())
            {
                allarmiSegnalati[key] = false;
            }

        }

        /// <summary>
        /// Registra e restituisce punto posizione attuale del Robot
        /// </summary>
        /// <returns></returns>
        public static DescPose RecPoint()
        {
            DescPose pos = new DescPose();

            // Salvo le posizioni registrate
            robot.GetActualTCPPose(flag, ref pos);

            RoundPositionDecimals(ref pos, 3);

            return pos;
        }

        /// <summary>
        /// Metodo che alza il robot in una posizione sicura per evitare ostacoli.
        /// </summary>
        /// <param name="z">Quota sicura a cui alzare il robot.</param>
        public static void RetractRobotToSafePosition(double zOffset)
        {
            try
            {
                // Get della posizione attuale del Robot
                DescPose currentPose = new DescPose();
                robot.GetActualTCPPose(1, ref currentPose);

                // Crea una nuova posizione con l'altezza sicura
                DescPose safePose = new DescPose
                {
                    tran = new DescTran
                    {
                        x = currentPose.tran.x,
                        y = currentPose.tran.y,
                        z = currentPose.tran.z + zOffset
                    },
                    rpy = currentPose.rpy // Mantiene gli stessi valori di rotazione
                };

                // Muove il robot alla nuova posizione sicura
                int result = robot.MoveCart(safePose, tool, user, vel, acc, ovl, blendT, config);

                GetRobotMovementCode(result);

            }
            catch (Exception ex)
            {
                // Log dell'errore in caso di eccezione
                log.Error($"Errore durante l'operazione di retraction del robot: {ex.Message}");
            }
        }

        /// <summary>
        /// Metodo che alza il robot in una posizione sicura per evitare ostacoli.
        /// </summary>
        /// <param name="z">Quota sicura a cui alzare il robot.</param>
        public static void RetractRobotBackward(double y)
        {
            try
            {
                // Get della posizione attuale del Robot
                DescPose currentPose = new DescPose();
                robot.GetActualTCPPose(1, ref currentPose);

                // Crea una nuova posizione con l'altezza sicura
                DescPose safePose = new DescPose
                {
                    tran = new DescTran
                    {
                        x = currentPose.tran.x,
                        y = y,
                        z = currentPose.tran.z
                    },
                    rpy = currentPose.rpy // Mantiene gli stessi valori di rotazione
                };

                // Muove il robot alla nuova posizione sicura
                int result = robot.MoveCart(safePose, tool, user, 5, 5, ovl, blendT, config);

                GetRobotMovementCode(result);

            }
            catch (Exception ex)
            {
                // Log dell'errore in caso di eccezione
                log.Error($"Errore durante l'operazione di retraction del robot: {ex.Message}");
            }
        }

        /// <summary>
        /// Esegue get del codice di movimento del robot
        /// </summary>
        /// <param name="result">Codice risultato del movimento del robot</param>
        private static void GetRobotMovementCode(int result)
        {
            if (result != 0) // Se il codice passato come parametro è diverso da 0, significa che il movimento ha generato un errore
            {
                // Get del codice di errore dal database
                DataRow code = RobotDAO.GetRobotMovementCode(ConnectionString, result);

                if (code != null) // Se il codice è presente nel dizionario nel database eseguo la get dei dettagli
                {
                    // Stampo messaggio di errore
                    CustomMessageBox.Show(
                        MessageBoxTypeEnum.ERROR,
                        "Errcode: " + code["Errcode"].ToString() + "\nDescribe: " + code["Describe"].ToString() + "\nProcessing method: " + code["Processing method"].ToString()
                        );

                    // Scrivo messaggio nel log
                    log.Error("Errcode: " + code["Errcode"].ToString() + "\nDescribe: " + code["Describe"].ToString() + "\nProcessing method: " + code["Processing method"].ToString());
                }
                else // Se il codice non è presente nel dizionario nel database stampo un errore generico
                {
                    CustomMessageBox.Show(
                       MessageBoxTypeEnum.ERROR,
                       "Errore generico durante il movimento del robot"
                       );

                    log.Error("Errore generico durante il movimento del robot");

                }
            }
        }

        /// <summary>
        /// Funzione per aspettare che il robot abbia finito di muoversi
        /// </summary>
        private static void WaitForRobotToFinish()
        {
            byte state = 0;
            while (true)
            {
                RobotManager.robot.GetRobotMotionDone(ref state);
                if (state == 1)
                {
                    break; // Esce dal ciclo quando il movimento è completato
                }
                Thread.Sleep(100); // Attende brevemente prima di ricontrollare lo stato
            }
        }

        /// <summary>
        /// Imposta le proprietà del robot prelevandole dal database.
        /// </summary>
        /// <returns>True se l'operazione ha successo, altrimenti False.</returns>
        public static bool SetRobotProperties()
        {
            try
            {
                log.Info("Inizio impostazione delle proprietà del robot dal database.");

                // Ottieni le proprietà del robot dal database
                DataTable dt_robotProperties = RobotDAO.GetRobotProperties(ConnectionString);
                if (dt_robotProperties == null)
                {
                    log.Error("La tabella delle proprietà del robot è nulla.");
                    return false;
                }

                // Estrai e assegna le proprietà del robot
                int speed = Convert.ToInt16(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_SPEED_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                float velocity = float.Parse(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_VELOCITY_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                float blend = float.Parse(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_BLEND_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                float acceleration = float.Parse(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_ACCELERATION_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                float ovl = float.Parse(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_OVL_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                int tool = Convert.ToInt16(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_TOOL_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                int user = Convert.ToInt16(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_USER_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                int weight = Convert.ToInt16(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_WEIGHT_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());
                int velRec = Convert.ToInt16(dt_robotProperties.Rows[RobotDAOSqlite.ROBOT_PROPERTIES_VELREC_ROW_INDEX]
                    [RobotDAOSqlite.ROBOT_PROPERTIES_VALUE_COLUMN_INDEX].ToString());

                // Creazione dell'oggetto robotProperties
                robotProperties = new RobotProperties(speed, velocity, blend, acceleration, ovl, tool, user, weight, velRec);

                /*
                 DescPose coord = new DescPose(
                    -1115.359,
                    212.482,
                    -541.592,
                    179.973,
                    0.010,
                    0.765
                    );

               robot.SetWObjCoord(1, coord);
                */

                log.Info($"SetRobotProperties completata: " +
                         $"\n Speed: {speed}" +
                         $"\n Velocity: {velocity}" +
                         $"\n Blend: {blend}" +
                         $"\n Acceleration: {acceleration}" +
                         $"\n Ovl: {ovl}" +
                         $"\n Tool: {tool}" +
                         $"\n User: {user}" +
                         $"\n Weight: {weight}" +
                         $"\n VelRec: {velRec}");

                // Modifica delle variabili statiche e globali di RobotManager
                RobotManager.speed = robotProperties.Speed;
                RobotManager.vel = robotProperties.Velocity;
                RobotManager.acc = robotProperties.Acceleration;
                RobotManager.ovl = robotProperties.Ovl;
                RobotManager.blendT = robotProperties.Blend;
                RobotManager.tool = robotProperties.Tool;
                RobotManager.user = robotProperties.User;
                RobotManager.weight = robotProperties.Weight;
                RobotManager.velRec = robotProperties.VelRec;

                return true;
            }
            catch (Exception ex)
            {
                log.Error("Errore durante SetRobotProperties: " + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Generazione evento da allarme ricevuto
        /// </summary>
        /// <param name="e"></param>
        protected static void OnAllarmeGenerato(EventArgs e)
        {
            AllarmeGenerato?.Invoke(null, e);
        }

        /// <summary>
        /// Generazione evento da allarmi resettati
        /// </summary>
        /// <param name="e"></param>
        protected static void OnAllarmeResettato(EventArgs e)
        {
            AllarmeResettato?.Invoke(null, e);
        }

        /// <summary>
        /// Generazione eventi
        /// </summary>
        public static void TriggerAllarmeGenerato()
        {
            OnAllarmeGenerato(EventArgs.Empty);
        }

        /// <summary>
        /// Trigger attivato quando vengono cancellati gli allarmi
        /// </summary>
        public static void TriggerAllarmeResettato()
        {
            OnAllarmeResettato(EventArgs.Empty);
        }

        /// <summary>
        /// Controlla la connessione con il robot
        /// </summary>
        /// <param name="updates"></param>
        public static void CheckRobotConnection(List<(string key, bool value, string type)> updates)
        {
            string ipAddress = RobotIpAddress;

            if (!PingIPAddress(ipAddress))
            {
                numAttempsRobotPing++;

                if (numAttempsRobotPing >= 2)
                {
                    AlarmManager.isRobotConnected = false;

                    if (!Connection_Robot_Error)
                    {
                        // Alzo bit per segnalare errore connessione al Robot

                        Connection_Robot_Error = true;
                    }

                    numAttempsRobotPing = 0;
                }
            }
            else
            {
                if (!AlarmManager.isRobotConnected) // Se era disconnesso, riconnetto
                {
                    RobotManager.robot.RPC(RobotManager.RobotIpAddress);
                    // robot.RobotEnable(1);
                    AlarmManager.isRobotConnected = true;
                }

                if (Connection_Robot_Error)
                {
                    Connection_Robot_Error = false;
                }
            }
        }

        /// <summary>
        /// Metodo che pinga un indirizzo IP e ritorna true se il ping ha successo.
        /// </summary>
        private static bool PingIPAddress(string ipAddress)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();
            options.DontFragment = true;

            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(data);
            int timeout = 120;

            try
            {
                PingReply reply = pingSender.Send(ipAddress, timeout, buffer, options);
                return reply.Status == IPStatus.Success;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Esegue ciclo di test per pick e place teglia
        /// </summary>
        public static async void PickAndPlaceTeglia()

        {
            #region Dichiarazione punti routine

            // Parametro necessario al comando MoveL
            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset

            int offsetAllontamento = 900;

            #region Punto home

            var home = ApplicationConfig.applicationsManager.GetPosition("pHomeTeglia", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);

            #endregion


            #region Prima fase

            #region Punto di pick

            JointPos jointPosPick = new JointPos(0, 0, 0, 0, 0, 0);
            var pick = ApplicationConfig.applicationsManager.GetPosition("pPickTeglia", "RM");
            DescPose descPosPick = new DescPose(pick.x, pick.y, pick.z, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick, -1, ref jointPosPick);

            #endregion

            #region Punto avvicinamento pick

            // Prima fase del ciclo
            JointPos jointPosApproachPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick = new DescPose(pick.x, pick.y - offsetAllontamento, pick.z - 40, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick, -1, ref jointPosApproachPick);

            #endregion

            #region Punto post pick

            JointPos jointPosPostPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPick = new DescPose(pick.x, pick.y, pick.z + 20, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPick, -1, ref jointPosPostPick);

            #endregion

            #region Punto allontanamento post pick

            JointPos jointPosAllontanamentoPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPick = new DescPose(descPosApproachPick.tran.x, descPosApproachPick.tran.y,
                descPosApproachPick.tran.z + 80, descPosApproachPick.rpy.rx, descPosApproachPick.rpy.ry, descPosApproachPick.rpy.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPick, -1, ref jointPosAllontanamentoPick);

            #endregion


            #region Punto di place

            JointPos jointPosPlace = new JointPos(0, 0, 0, 0, 0, 0);
            var place = ApplicationConfig.applicationsManager.GetPosition("pPlaceTeglia", "RM");
            DescPose descPosPlace = new DescPose(place.x, place.y, place.z, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPlace, -1, ref jointPosPlace);

            #endregion

            #region Punto avvicinamento place

            JointPos jointPosApproachPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPlace = new DescPose(place.x + 10, place.y - offsetAllontamento, place.z + 50,
                place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPlace, -1, ref jointPosApproachPlace);

            #endregion

            #region Punto post place

            JointPos jointPosPostPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPlace = new DescPose(place.x, place.y, place.z,
                place.rx - 3, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPlace, -1, ref jointPosPostPlace);

            #endregion

            #region Punto allontanamento place

            JointPos jointPosAllontanamentoPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPlace = new DescPose(place.x, place.y - offsetAllontamento, place.z,
                place.rx - 3, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPlace, -1, ref jointPosAllontanamentoPlace);

            #endregion

            #endregion

            #region Seconda fase

            #region Punto avvicinamento pick

            // Prima fase del ciclo
            JointPos jointPosApproachPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick2 = new DescPose(place.x, place.y - offsetAllontamento, place.z - 40, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick2, -1, ref jointPosApproachPick2);

            #endregion

            #region Punto di pick

            JointPos jointPosPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPick2 = new DescPose(place.x, place.y, place.z, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick2, -1, ref jointPosPick2);

            #endregion

            #region Punto post pick

            JointPos jointPosPostPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPick2 = new DescPose(place.x, place.y, place.z + 20, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPick2, -1, ref jointPosPostPick2);

            #endregion

            #region Punto allontanamento post pick

            JointPos jointPosAllontanamentoPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPick2 = new DescPose(descPosApproachPick2.tran.x, descPosApproachPick2.tran.y,
                descPosApproachPick2.tran.z + 80, descPosApproachPick2.rpy.rx, descPosApproachPick2.rpy.ry, descPosApproachPick2.rpy.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPick2, -1, ref jointPosAllontanamentoPick2);

            #endregion


            #region Punto avvicinamento place

            JointPos jointPosApproachPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPlace2 = new DescPose(pick.x, pick.y - offsetAllontamento, pick.z + 50,
                pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPlace2, -1, ref jointPosApproachPlace2);

            #endregion

            #region Punto di place

            JointPos jointPosPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPlace2 = new DescPose(pick.x - 10, pick.y, pick.z, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPlace2, -1, ref jointPosPlace2);

            #endregion

            #region Punto post place

            JointPos jointPosPostPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPlace2 = new DescPose(pick.x, pick.y, pick.z,
                pick.rx - 3, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPlace2, -1, ref jointPosPostPlace2);

            #endregion

            #region Punto allontanamento place

            JointPos jointPosAllontanamentoPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPlace2 = new DescPose(pick.x, pick.y - offsetAllontamento, pick.z,
                pick.rx - 3, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPlace2, -1, ref jointPosAllontanamentoPlace2);

            #endregion

            #endregion

            #endregion

            #region Parametri movimento

            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            // Indica il codice risultante del movimento del Robot
            int movementResult = -1;

            // Reset condizione di stop ciclo
            stopCycleRoutine = false;

            // Reset richiesta di stop ciclo
            stopCycleRequested = false;

            // Reset step routine
            step = 0;

            // Dichiarazione stato della pinza
            int gripperStatus = -1;

            // Segnale di pick
            bool prendidaNastro = true;

            // Segnale di place
            bool appoggiaSuScatola = true;

            #endregion


            // Aspetto che il metodo termini, ma senza bloccare il thread principale
            // La routine è incapsulata come 'async' per supportare futuri operatori 'await' nel caso ci fosse la necessità
            await Task.Run(async () =>
            {
                // Fino a quando la condizione di stop routine non è true e non sono presenti allarmi bloccanti
                while (!stopCycleRoutine && !AlarmManager.blockingAlarm)
                {
                    switch (step)
                    {
                        case 0:
                            #region Check richiesta interruzione ciclo

                            if (!stopCycleRequested) // Se non è stata richiesta nessuna interruzione
                            {
                                step = 10;
                            }
                            else // Se è stata richiesta l'interruzione
                            {
                                // Ritorno del Robot a casa
                                GoToHomePosition();

                                // Reset inPosition
                                inPosition = false;

                                // Assegnazione del pHome come ending point
                                endingPoint = descPosHome;

                                step = 5;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 0 - Check richiesta interruzione ciclo");
                            break;

                        #endregion

                        case 5:
                            #region Termine routine

                            if (inPosition) // Se il Robot è arrivato in HomePosition
                            {
                                // Abilito il tasto Start per avviare nuovamente la routine
                                EnableButtonCycleEvent?.Invoke(1, EventArgs.Empty);


                                // Imposto a false il booleano che fa terminare il thread della routine
                                stopCycleRoutine = true;

                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 5 - Termine routine");
                            break;

                        #endregion

                        case 10:
                            #region Movimento a punto di Pick

                            inPosition = false; // Reset inPosition

                            // Invio punto di Home
                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config);
                            GetRobotMovementCode(movementResult);

                            // Invio punto di avvicinamento Pick
                            movementResult = robot.MoveCart(descPosApproachPick, tool, user, vel, acc,
                                ovl, blendT, config);
                            GetRobotMovementCode(movementResult);

                            // Movimento lineare finale in Pick
                            movementResult = robot.MoveL(jointPosPick, descPosPick, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult);

                            endingPoint = descPosPick; // Assegnazione endingPoint
                            step = 20;

                            formDiagnostics.UpdateRobotStepDescription("STEP 10 - Movimento a punto di Pick (prima fase)");

                            break;
                        #endregion


                        case 20:
                            #region Delay per calcolo in position punto di pick

                            Thread.Sleep(500);
                            step = 30;
                            formDiagnostics.UpdateRobotStepDescription("STEP 20 -  Delay calcolo in position punto di pick (prima fase)");
                            break;

                        #endregion

                        case 30:
                            #region Attesa inPosition punto di Pick

                            if (inPosition) // Se il Robot è arrivato in posizione di Pick
                            {
                                Thread.Sleep(500);
                                // Chiudo la pinza
                              
                                step = 40;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 30 - Attesa inPosition punto di Pick (prima fase)");
                            break;

                        #endregion

                        case 40:
                            #region Presa teglia

                            // if (gripperStatus == 0)
                            {
                                Thread.Sleep(1000); // Per evitare "rimbalzo" del Robot
                                step = 50;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 40 - Presa teglia (prima fase)");
                            break;

                        #endregion

                        case 50:
                            #region Movimento a punto di Home

                            movementResult = robot.MoveL(jointPosPostPick, descPosPostPick, tool, user, vel, acc,
                                   ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult);

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosAllontanamentoPick, descPosAllontanamentoPick, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            formDiagnostics.UpdateRobotStepDescription("STEP 50 - Movimento a punto di Home");

                            step = 60;
                            break;

                        #endregion

                        case 60:
                            #region Movimento a punto di place

                            if (appoggiaSuScatola)
                            {
                                inPosition = false; // Reset inPosition

                                movementResult = robot.MoveCart(descPosApproachPlace, tool, user, vel, acc,
                                  ovl, blendT, config); // Invio punto di avvicinamento place
                                GetRobotMovementCode(movementResult);

                                // Movimento lineare
                                movementResult = robot.MoveL(jointPosPlace, descPosPlace, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset
                                    );

                                endingPoint = descPosPlace; // Assegnazione endingPoint

                                step = 70;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 60 - Movimento a punto di place (prima fase)");
                            break;

                        #endregion

                        case 70:
                            #region Delay per calcolo in position punto di place

                            Thread.Sleep(500);
                            step = 80;
                            formDiagnostics.UpdateRobotStepDescription("STEP 70 -  Delay calcolo in position punto di place (prima fase)");
                            break;

                        #endregion

                        case 80:
                            #region Attesa inPosition punto di Place

                            if (inPosition) // Se il Robot è arrivato in posizione di place
                            {
                                Thread.Sleep(500);
                                // Chiudo la pinza
      
                                step = 90;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 80 - Attesa inPosition punto di place (prima fase)");
                            break;
                        #endregion

                        case 90:
                            #region Rilascio teglia

                            // if (gripperStatus == 0)
                            {
                                Thread.Sleep(1000); // Per evitare "rimbalzo" del Robot
                                step = 100;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 90 - Rilascio teglia (prima fase)");
                            break;

                        #endregion

                        case 100:
                            #region Movimento a punto di Home

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosPostPlace, descPosPostPlace, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosAllontanamentoPlace, descPosAllontanamentoPlace, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            formDiagnostics.UpdateRobotStepDescription("STEP 100 - Movimento a punto di Home");

                            step = 110;
                            break;
                        #endregion

                        case 110:
                            #region Movimento a punto di pick

                            inPosition = false; // Reset inPosition

                            movementResult = robot.MoveCart(descPosApproachPick2, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di avvicinamento place
                            GetRobotMovementCode(movementResult);

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosPick2, descPosPick2, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            endingPoint = descPosPick2; // Assegnazione endingPoint

                            step = 120;

                            formDiagnostics.UpdateRobotStepDescription("STEP 110 - Movimento a punto di pick (seconda fase)");
                            #endregion
                            break;

                        case 120:
                            #region Delay per calcolo in position punto di pick

                            Thread.Sleep(500);
                            step = 130;
                            formDiagnostics.UpdateRobotStepDescription("STEP 120 -  Delay calcolo in position punto di pick (seconda fase)");
                            break;

                        #endregion

                        case 130:
                            #region Attesa inPosition punto di pick

                            if (inPosition) // Se il Robot è arrivato in posizione di place
                            {
                                Thread.Sleep(500);
                                // Chiudo la pinza
                            
                                step = 140;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 130 - Attesa inPosition punto di pick (seconda fase)");
                            break;
                        #endregion

                        case 140:
                            #region Presa teglia

                            // if (gripperStatus == 0)
                            {
                                Thread.Sleep(1000); // Per evitare "rimbalzo" del Robot
                                step = 150;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 140 - Presa teglia (seconda fase)");
                            break;

                        #endregion

                        case 150:
                            #region Movimento a punto di Home e riavvio ciclo

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosPostPick2, descPosPostPick2, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosAllontanamentoPick2, descPosAllontanamentoPick2, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            formDiagnostics.UpdateRobotStepDescription("STEP 150 - Movimento a punto di Home");

                            step = 160;
                            break;
                        #endregion

                        case 160:
                            #region Movimento a punto di Place

                            if (prendidaNastro)
                            {
                                inPosition = false; // Reset inPosition

                                // STEP 1: Invio punto di Home
                                movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);

                                // STEP 3: Invio punto di avvicinamento Pick
                                movementResult = robot.MoveCart(descPosApproachPlace2, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);

                                // STEP 4: Movimento lineare finale
                                movementResult = robot.MoveL(jointPosPlace2, descPosPlace2, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset);
                                GetRobotMovementCode(movementResult);

                                endingPoint = descPosPlace2; // Assegnazione endingPoint
                                step = 170;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 160 - Movimento a punto di Place (seconda fase)");
                            break;
                        #endregion


                        case 170:
                            #region Delay per calcolo in position punto di place

                            Thread.Sleep(500);
                            step = 180;
                            formDiagnostics.UpdateRobotStepDescription("STEP 170 -  Delay calcolo in position punto di place (seconda fase)");
                            break;

                        #endregion

                        case 180:
                            #region Attesa inPosition punto di Place

                            if (inPosition) // Se il Robot è arrivato in posizione di Pick
                            {
                                Thread.Sleep(500);
                                // Chiudo la pinza
                               
                                step = 190;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 180 - Attesa inPosition punto di place (seconda fase)");
                            break;

                        #endregion

                        case 190:
                            #region Rilascio teglia

                            // if (gripperStatus == 0)
                            {
                                Thread.Sleep(1000); // Per evitare "rimbalzo" del Robot
                                step = 200;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 190 - Rilascio teglia (seconda fase)");
                            break;

                        #endregion

                        case 200:
                            #region Movimento a punto di Home e riavvio ciclo

                            movementResult = robot.MoveL(jointPosPostPlace2, descPosPostPlace2, tool, user, vel, acc,
                                   ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult);

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosAllontanamentoPlace2, descPosAllontanamentoPlace2, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            formDiagnostics.UpdateRobotStepDescription("STEP 200 - Movimento a punto di Home e riavvio ciclo");

                            step = 0;
                            break;

                            #endregion
                    }

                    Thread.Sleep(10); // Delay routine
                }
            });
        }

        /// <summary>
        /// Esegue metodo che gestisce routine di pick e di place
        /// </summary>
        public static async Task MainCycle()
        {
            #region Variabili necessarie per funzionamento ciclo

            // Reset condizione di stop ciclo
            stopCycleRoutine = false;

            // Reset richiesta di stop ciclo
            stopCycleRequested = false;

            // Reset step routine
            step = 0;

            // Segnale di pick
            int execPick = 0;

            // Segnale di place
            int execPlace = 0;

            // Consensi di pick
            int enableToPick = 0;

            // Consensi di place
            int enableToPlace = 0;

            #endregion

            double[] levelCollision1 = new double[] { 1, 1, 1, 1, 1, 1 };
            double[] levelCollision2 = new double[] { 2, 2, 2, 2, 2, 2 };
            double[] levelCollision3 = new double[] { 3, 3, 3, 3, 3, 3 };
            double[] levelCollision4 = new double[] { 4, 4, 4, 4, 4, 4 };
            double[] levelCollision5 = new double[] { 5, 5, 5, 5, 5, 5 };
            double[] levelCollision6 = new double[] { 6, 6, 6, 6, 6, 6 };
            double[] levelCollision7 = new double[] { 7, 7, 7, 7, 7, 7 };
            double[] levelCollision8 = new double[] { 8, 8, 8, 8, 8, 8 };

            robot.SetAnticollision(0, levelCollision3, 1);
            currentCollisionLevel = 3;

            // Aspetto che il metodo termini, ma senza bloccare il thread principale
            // La routine è incapsulata come 'async' per supportare futuri operatori 'await' nel caso ci fosse la necessità
            await Task.Run(async () =>
            {
                // Fino a quando la condizione di stop routine non è true e non sono presenti allarmi bloccanti
                while (!stopCycleRoutine && !AlarmManager.blockingAlarm) 
                {
                    switch (step)
                    {
                        case 0:
                            #region Check richiesta interruzione ciclo
                            // In questo step scrivo al plc che il ciclo di main è stato avviato e passo subito allo step successivo

                            // Aggiorno la variabile globale e statica che scrivo al PLC nel metodo SendUpdatesToPLC 
                            // per informare il plc che il ciclo main è in esecuzione
                            CycleRun_Main = 1;

                            // Passaggio allo step 10
                            step = 10;

                            break;

                        #endregion

                        case 10:
                            #region Check richiesta routine
                            // In questo step leggo dal plc se è arrivata una richiesta di pick o di place e se ci sono i consensi per procedere
                            // ai relativi step successivi

                            #region Place

                            // Get comando di place da plc
                            execPlace = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_Place));

                            // Get consensi di place da plc
                            enableToPlace = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.Enable_To_Place));

                            if (execPlace == 1) // Check richiesta di place routine
                            {
                                if (enableToPlace == 1) // Check consensi place
                                {
                                    step = 30; // Passaggio allo step dedicato al place
                                }
                            }

                            #endregion

                            #region Pick

                            // Get comando di pick da plc
                            execPick = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_Pick));

                            // Get consensi di pick da plc
                            enableToPick = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.Enable_To_Pick));

                            if (execPick == 1) // Check richiesta di pick routine
                            {
                                if (enableToPick == 1) // Check consensi pick
                                {
                                    step = 20; // Passaggio allo step dedicato al pick
                                }
                            }

                            #endregion

                            break;

                        #endregion

                        case 20:
                            #region Get punto di pick
                            // In questo step eseguo la get del punto di pick dal dizionario generato tramite database
                            // e controllo prima se il punto è presente nel dizionario e dopo se le coordinate sono valide
                            // verificando che tutte siano diverse da 0

                            int box = (Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_SelectedFormat)) % 1000);
                            if (box > 100 && box < 302)
                            {

                                // Get del punto di pick dal dizionario
                                var pick = ApplicationConfig.applicationsManager.GetPosition((Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_SelectedFormat)) % 1000).ToString(), "RM");

                                // Check su presenza del punto nel dizionario
                                if (pick != null)
                                {
                                    // Check validità del punto
                                    if (pick.x != 0 && pick.y != 0 && pick.z != 0 && pick.rx != 0 && pick.ry != 0 && pick.rz != 0)
                                    {
                                        await PickBox(pick); // Eseguo e attendo sia terminata la routine di pick
                                        step = 10; // Riavvio del ciclo per controllare nuove richieste di pick o place
                                    }
                                }
                                else // Se il punto non è presente nel dizionario o non ha coordinate valide
                                {
                                    step = 1001; // Passaggio allo step dedicato all'errore del pick
                                }
                            }
                            else
                            {
                                step = 1001; // Passaggio allo step dedicato all'errore del pick
                            }

                            break;

                        #endregion

                        case 30:
                            #region Get punto di place
                            // In questo step eseguo la get del punto di place dal dizionario generato tramite database
                            // e controllo prima se il punto è presente nel dizionario e dopo se le coordinate sono valide
                            // verificando che tutte siano diverse da 0

                            // Eseguo get del nome del punto di place che proviene dal plc.
                            // La codifica è:
                            // Migliata -> Numero pallet
                            // Centinaia -> Formato scatola
                            // Unità -> Numero scatola
                            // Es: 1101 -> Pallet 1, formato scatola 1, prima scatola
                            int selectedFormat = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_SelectedFormat));

                            if (selectedFormat > 1100 && selectedFormat < 2302)
                            {

                                // Eseguo get del punto di place dal dizionario tramite la codifica ricevuta dal plc
                                var place = ApplicationConfig.applicationsManager.GetPosition(selectedFormat.ToString(), "RM");

                                // Check su presenza del punto nel dizionario
                                if (place != null)
                                {
                                    // Check validità del punto
                                    if (place.x != 0 && place.y != 0 && place.z != 0 && place.rx != 0 && place.ry != 0 && place.rz != 0) // Se il punto è valido
                                    {
                                        await PlaceBox(place); // Eseguo e attendo sia terminata la routine di place
                                        step = 10; // Riavvio del ciclo per controllare nuove richieste di pick o place
                                    }
                                }
                                else // Se il punto non è presente nel dizionario o non ha coordinate valide
                                {
                                    step = 1002; // Passaggio allo step dedicato all'errore del place
                                }
                            }
                            else
                            {
                                step = 1002; // Passaggio allo step dedicato all'errore del place
                            }

                            break;

                        #endregion

                        case 1001:
                            #region Errore punto di pick
                            // In questa fase stampiamo semplicemente l'errore relativo al punto di pick sul log

                            log.Error("Punto di pick non presente nel dizionario"); // Stampa messaggio di errore

                            break;

                        #endregion

                        case 1002:
                            #region Errore punto di place
                            // In questa fase stampiamo semplicemente l'errore relativo al punto di place sul log

                            log.Error("Punto di place non presente nel dizionario"); // Stampa messaggio di errore

                            break;

                            #endregion
                    }

                    Thread.Sleep(100); // Delay routine
                }
            });
        }
    
        /// <summary>
        /// Esegue il pick della scatola
        /// </summary>
        /// <returns></returns>
        public static async Task PickBox(ApplicationPositions pick)
        {
            stepPick = 0; // Step ciclo di pick
            stopPickRoutine = false; // Segnale di stop della pick routine
            int movementResult = 0; // Risultato del movimento del Robot

            #region Dichiarazione punti routine

            #region Punto home

            var home = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);

            #endregion

            #region Punto di pick

            int xOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_X));
            int yOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_Y));
            int zOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Pick_Z));

            JointPos jointPosPick = new JointPos(0, 0, 0, 0, 0, 0);
            //var pick = ApplicationConfig.applicationsManager.GetPosition((Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_SelectedFormat)) % 1000).ToString(), "RM");
            DescPose descPosPick = new DescPose(pick.x + xOffset, pick.y + yOffset, pick.z + zOffset, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick, -1, ref jointPosPick);

            #endregion

            #region Punto avvicinamento pick

            JointPos jointPosApproachPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick = new DescPose(pick.x, pick.y, pick.z + 200, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick, -1, ref jointPosApproachPick);

            #endregion

            #endregion

            #region Parametri movimento

            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset
            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            #endregion

            // Aspetto che il metodo termini, ma senza bloccare il thread principale
            // La routine è incapsulata come 'async' per supportare futuri operatori 'await' nel caso ci fosse la necessità
            await Task.Run(async () =>
            {
                // Fino a quando la condizione di stop routine non è true e non sono presenti allarmi bloccanti
                while (!stopPickRoutine && !AlarmManager.blockingAlarm)
                {
                    switch (stepPick)
                    {
                        case 0:
                            #region Movimento a punto di Pick
                            // In questo step avviso il plc che il ciclo di pick è avviato, eseguo i punti
                            // di avvicinamento pick, pick e imposto come ending point il punto di pick
                            // per il controllo del metodo di inPosition

                            // Aggiorno la variabile globale e statica che scrivo al PLC nel metodo SendUpdatesToPLC 
                            // per informare il plc che il ciclo pick è in esecuzione
                            CycleRun_Pick = 1;

                            inPosition = false; // Reset inPosition

                            // Invio punto di avvicinamento Pick
                            movementResult = robot.MoveCart(descPosApproachPick, tool, user, vel, acc, ovl, blendT, config);
                            GetRobotMovementCode(movementResult); // Get del risultato del movimento del robot

                            // Invio punto di pick
                            movementResult = robot.MoveL(jointPosPick, descPosPick, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult); // Get del risultato del movimento del robot

                            endingPoint = descPosPick; // Assegnazione endingPoint

                            stepPick = 10; // Passaggio allo step successivo

                            break;

                        #endregion

                        case 10:
                            #region Delay per calcolo in position punto di pick
                            // In questo step applico un leggero ritardo per facilitare
                            // calcolo dell'inPosition

                            Thread.Sleep(500); // Ritardo di 500ms

                            stepPick = 20; // Passaggio allo step successivo

                            break;

                        #endregion

                        case 20:
                            #region Attesa inPosition punto di Pick
                            // In questo step attendo che il robot arrivi nella posizione di pick

                            if (inPosition) // Se il Robot è arrivato in posizione di Pick
                            {
                                stepPick = 30; // Passaggio allo step successivo
                            }
                           
                            break;

                        #endregion

                        case 30:
                            #region Check presa ventosa
                            // In questo step verifico che la ventosa si sia attivata

                            Thread.Sleep(500);

                            stepPick = 40; // Passaggio allo step successivo

                            break;

                        #endregion

                        case 40:
                            #region Movimento a punto di Home

                            inPosition = false; // Reset inPosition

                            // Invio punto allontanamento pick
                            movementResult = robot.MoveL(jointPosApproachPick, descPosApproachPick, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult);

                            // Invio punto di home
                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc, ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            endingPoint = descPosHome;

                            stepPick = 50;

                            break;

                        #endregion

                        case 50:
                            #region Attesa inPosition punto di Home

                            if (inPosition) // Se è arrivato in home termino la routine
                            {
                                stepPick = 99;
                            }

                            break;

                        #endregion

                        case 99:
                            #region Attesa reset comando di pick

                            int pickSignal = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_Pick)); // Get segnale di place

                            if (pickSignal == 0) // Se è stato resettato, termino la routine
                            {
                                stepPick = 0;
                                CycleRun_Pick = 0;
                                stopPickRoutine = true;
                            }

                            #endregion

                            break;
                    }

                    Thread.Sleep(200); // Delay routine
                }
            });
        }

        public static async Task HomeRoutine()
        {
            // Get del punto di home
            var restPose = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            DescPose pHome = new DescPose(restPose.x, restPose.y, restPose.z, restPose.rx, restPose.ry, restPose.rz);

            stopHomeRoutine = false; // Reset segnale di stop ciclo home
            stepHomeRoutine = 0; // Reset degli step della HomeRoutine

            // Avvia un task separato per il ciclo while
            await Task.Run(async () =>
            {
                while (!stopHomeRoutine) // Fino a quando non termino la home routine
                {

                    switch (stepHomeRoutine)
                    {
                        case 0:
                            #region Cancellazione coda Robot e disattivazione tasti applicazione

                            CycleRun_Home = 1;

                            SetHomeRoutineSpeed();
                            await Task.Delay(1000);

                            stepHomeRoutine = 10;

                            break;

                        #endregion

                        case 10:
                            #region Movimento a punto di home

                            MoveRobotToSafePosition();
                            GoToHomePosition();
                            endingPoint = pHome;

                            stepHomeRoutine = 20;

                            break;

                        #endregion

                        case 20:
                            #region Attesa inPosition home

                            if (inPosition)
                                stepHomeRoutine = 99;

                            break;

                        #endregion

                        case 99:
                            #region Termine ciclo e riattivazione tasti applicazione e tasto home 

                            int homeStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_GoHome));

                            if (homeStatus == 0)
                            {
                                ResetHomeRoutineSpeed();

                                CycleRun_Home = 0;
                                stepHomeRoutine = 0;
                                stopHomeRoutine = true;
                            }
                            break;

                            #endregion


                    }


                    await Task.Delay(100); // Delay routine
                }
            });

        }

        /// <summary>
        /// Esegue il place della scatola
        /// </summary>
        /// <returns></returns>
        public static async Task PlaceBox(ApplicationPositions place)
        {
            stepPlace = 0; // Step ciclo di place
            int movementResult = 0; // Risultato del movimento del Robot
            int gripperStatus = 0; // Stato della ventosa
            stopPlaceRoutine = false; // Segnale di stop della place routine
            int numeroRighe = (int)(larghezzaPallet / larghezzaScatola); // Numero di righe di scatole su pallet
            int numeroColonne = (int)(lunghezzaPallet / lunghezzaScatola); // Numero di colonne di scatole su pallet
            bool stepWritten = false;

            #region Dichiarazione dei punti

            #region Punto home

            var home = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);

            #endregion

            #region Punto di place

           // int selectedFormat = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_SelectedFormat));
            int rotate180 = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_Box_Rotate_180));

            int xOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Place_X));
            int yOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Place_Y));
            int zOffset = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.OFFSET_Place_Z));

            JointPos jointPosPlace = new JointPos(0, 0, 0, 0, 0, 0);
            //var place = ApplicationConfig.applicationsManager.GetPosition(selectedFormat.ToString(), "RM");
            DescPose descPosPlace = new DescPose(place.x + xOffset, place.y + yOffset, place.z + zOffset, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPlace, -1, ref jointPosPlace);

            if (rotate180 == 1)
            {
                descPosPlace.rpy.rz = NormalizeAngle((float)descPosPlace.rpy.rz + 180);
            }

            #endregion

            #region Punto avvicinamento place

            JointPos jointPosApproachPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPlace = new DescPose(place.x, place.y, place.z + 200, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPlace, -1, ref jointPosApproachPlace);

            #endregion

            DescPose originePallet = descPosPlace;
            JointPos jointPosPlaceCalculated = new JointPos(0, 0, 0, 0, 0, 0);

            #endregion

            #region Parametri movimento

            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset
            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            #endregion

            await Task.Run(async () =>
            {
                // Fino a quando la condizione di stop routine non è true e non sono presenti allarmi bloccanti
                while (!stopPlaceRoutine && !AlarmManager.blockingAlarm)
                {
                    switch (stepPlace)
                    {
                        case 0:
                            #region Movimento a punto di place

                            CycleRun_Place = 1;

                            inPosition = false; // Reset inPosition
                           
                            // Invio punto di avvicinamento place
                            movementResult = robot.MoveCart(descPosApproachPlace, tool, user, vel, acc, ovl, blendT, config); // Invio punto di avvicinamento place
                            GetRobotMovementCode(movementResult);

                            // Invio punto di place
                            movementResult = robot.MoveL(jointPosPlace, descPosPlace, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset );

                            endingPoint = descPosPlace; // Assegnazione endingPoint

                            stepPlace = 10;
                           
                            break;

                        #endregion

                        case 10:
                            #region Delay per calcolo in position punto di place

                            Thread.Sleep(500);
                            stepPlace = 20;

                            break;

                        #endregion

                        case 20:
                            #region Attesa inPosition punto di Place

                            if (inPosition) // Se il Robot è arrivato in posizione di place
                            {                                                      
                                stepPlace = 30;
                            }

                            break;

                        #endregion

                        case 30:
                            #region Rilascio scatola su pallet

                            {
                                Thread.Sleep(500); // Per evitare "rimbalzo" del Robot
                                stepPlace = 40;
                            }

                            break;

                        #endregion

                        case 40:
                            #region Movimento a punto di Home

                            inPosition = false; // Reset inPosition

                            // Invio punto di allontamento place
                            movementResult = robot.MoveL(jointPosApproachPlace, descPosApproachPlace, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset );
                            GetRobotMovementCode(movementResult);

                            // Invio punto di place
                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc, ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            endingPoint = descPosHome;

                            stepPlace = 50;

                            break;

                        #endregion

                        case 50:
                            #region Attesa inPosition punto di Home

                            if (inPosition) // Se è arrivato in home termino la routine
                            {
                                stepPlace = 99;
                            }

                            break;

                        #endregion

                        case 99:
                            #region Attesa reset comando di place

                          

                            int placeSignal = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.CMD_Place)); // Get segnale di place

                            if (placeSignal == 0) // Se è stato resettato, termino la routine
                            {
                                stepPlace = 0;
                                CycleRun_Place = 0;
                                stopPlaceRoutine = true;
                            }

                            #endregion

                            break;

                    }

                    Thread.Sleep(200); // Delay routine
                }
            });
        }

        /// <summary>
        /// Esegue ciclo di pick di una scatola e place dentro al pallet in una determinata posizione
        /// </summary>
        public static async void PickAndPlaceScatola()
        {
            /*if(larghezzaScatola == 0 || larghezzaPallet == 0) // controllare che non ci siano valori = 0 prima di iniziare
            {
                return;
            }*/

            #region Dichiarazione punti routine

            #region Punto home

            var home = ApplicationConfig.applicationsManager.GetPosition("pHome", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);

            #endregion

            #region Punto di pick

            JointPos jointPosPick = new JointPos(0, 0, 0, 0, 0, 0);
            var pick = ApplicationConfig.applicationsManager.GetPosition("pPick", "RM");
            DescPose descPosPick = new DescPose(pick.x, pick.y, pick.z, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick, -1, ref jointPosPick);

            #endregion

            #region Punto avvicinamento pick

            JointPos jointPosApproachPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick = new DescPose(pick.x, pick.y, pick.z + 200, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick, -1, ref jointPosApproachPick);

            #endregion

            #region Punto di place

            JointPos jointPosPlace = new JointPos(0, 0, 0, 0, 0, 0);
            var place = ApplicationConfig.applicationsManager.GetPosition("pPlace", "RM");
            DescPose descPosPlace = new DescPose(place.x, place.y, place.z, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPlace, -1, ref jointPosPlace);

            #endregion

            #region Punto avvicinamento place

            JointPos jointPosApproachPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPlace = new DescPose(place.x, place.y, place.z + 200, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPlace, -1, ref jointPosApproachPlace);

            #endregion

            #endregion

            #region Parametri movimento

            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset
            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            #endregion

            // Indica il codice risultante del movimento del Robot
            int movementResult = -1;

            // Reset condizione di stop ciclo
            stopCycleRoutine = false;

            // Reset richiesta di stop ciclo
            stopCycleRequested = false;

            // Reset step routine
            step = 0;

            // Dichiarazione stato della pinza
            int gripperStatus = -1;

            // Segnale di pick
            bool prendidaNastro = true;

            // Segnale di place
            bool appoggiaSuPallet = true;


            int numeroRighe = (int)(larghezzaPallet / larghezzaScatola);
            int numeroColonne = (int)(lunghezzaPallet / lunghezzaScatola);

            // CalcolaPosizioneFocacce(larghezzaFocaccia,profonditaFocaccia,larghezzaPallet,profonditaPallet);

            DescPose originePallet = descPosPlace;
            JointPos jointPosPlaceCalculated = new JointPos(0, 0, 0, 0, 0, 0);

            // Aspetto che il metodo termini, ma senza bloccare il thread principale
            // La routine è incapsulata come 'async' per supportare futuri operatori 'await' nel caso ci fosse la necessità
            await Task.Run(async () =>
            {
                // Fino a quando la condizione di stop routine non è true e non sono presenti allarmi bloccanti
                while (!stopCycleRoutine && !AlarmManager.blockingAlarm)
                {
                    switch (step)
                    {
                        case 0:
                            #region Check richiesta interruzione ciclo

                            if (!stopCycleRequested) // Se non è stata richiesta nessuna interruzione
                            {
                                step = 10;
                            }
                            else // Se è stata richiesta l'interruzione
                            {
                                // Ritorno del Robot a casa
                                GoToHomePosition();

                                // Reset inPosition
                                inPosition = false;

                                // Assegnazione del pHome come ending point
                                endingPoint = descPosHome;

                                step = 5;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 0 - Check richiesta interruzione ciclo");
                            break;

                        #endregion

                        case 5:
                            #region Termine routine

                            if (inPosition) // Se il Robot è arrivato in HomePosition
                            {
                                // Abilito il tasto Start per avviare nuovamente la routine
                                EnableButtonCycleEvent?.Invoke(1, EventArgs.Empty);

                                // Imposto a false il booleano che fa terminare il thread della routine
                                stopCycleRoutine = true;

                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 5 - Termine routine");
                            break;

                        #endregion

                        case 10:
                            #region Movimento a punto di Pick

                            if (prendidaNastro)
                            {
                                inPosition = false; // Reset inPosition

                                // STEP 1: Invio punto di Home
                                movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);

                                // STEP 3: Invio punto di avvicinamento Pick
                                movementResult = robot.MoveCart(descPosApproachPick, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);

                                // STEP 4: Movimento lineare finale
                                movementResult = robot.MoveL(jointPosPick, descPosPick, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset);
                                GetRobotMovementCode(movementResult);

                                endingPoint = descPosPick; // Assegnazione endingPoint
                                step = 20;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 10 - Movimento a punto di Pick");
                            break;
                        #endregion

                        case 20:
                            #region Delay per calcolo in position punto di pick

                            Thread.Sleep(500);
                            step = 30;
                            formDiagnostics.UpdateRobotStepDescription("STEP 20 -  Delay calcolo in position punto di pick");
                            break;

                        #endregion

                        case 30:
                            #region Attesa inPosition punto di Pick

                            if (inPosition) // Se il Robot è arrivato in posizione di Pick
                            {
                                // Chiudo la pinza
                      
                                step = 40;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 30 - Attesa inPosition punto di Pick");
                            break;

                        #endregion

                        case 40:
                            #region Presa focaccia

                            // if (gripperStatus == 0)
                            {
                                Thread.Sleep(500); // Per evitare "rimbalzo" del Robot
                                step = 50;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 40 - Presa focaccia");
                            break;

                        #endregion

                        case 50:
                            #region Movimento a punto di Home

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosApproachPick, descPosApproachPick, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            /*
                            // Movimento cartesiano
                            movementResult = robot.MoveCart(descPosApproachPick, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di avvicinamento Pick
                            GetRobotMovementCode(movementResult);
                            */
                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            formDiagnostics.UpdateRobotStepDescription("STEP 50 - Movimento a punto di Home");

                            step = 60;
                            break;

                        #endregion

                        case 60:
                            #region Movimento a punto di place

                            if (appoggiaSuPallet)
                            {
                                inPosition = false; // Reset inPosition

                                DescPose puntoPlaceScatola = CalcolaPosizioneScatola(
                                    riga,
                                    colonna,
                                    strato,
                                    larghezzaScatola,
                                    lunghezzaScatola,
                                    altezzaScatola,
                                    originePallet
                                );

                                RobotManager.robot.GetInverseKin(0, puntoPlaceScatola, -1, ref jointPosPlaceCalculated);

                                descPosApproachPlace = new DescPose(
                                    puntoPlaceScatola.tran.x,
                                    puntoPlaceScatola.tran.y,
                                    puntoPlaceScatola.tran.z + 200,
                                    puntoPlaceScatola.rpy.rx,
                                    puntoPlaceScatola.rpy.ry,
                                    puntoPlaceScatola.rpy.rz);

                                RobotManager.robot.GetInverseKin(0, descPosApproachPlace, -1, ref jointPosApproachPlace);

                                movementResult = robot.MoveCart(descPosApproachPlace, tool, user, vel, acc,
                                  ovl, blendT, config); // Invio punto di avvicinamento place
                                GetRobotMovementCode(movementResult);

                                // Movimento lineare
                                movementResult = robot.MoveL(jointPosPlaceCalculated, puntoPlaceScatola, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset
                                    );

                                //TODO: questi calcoli non verranno più fatti qui ma direttamente del PLC, noi leggiamo e basta i valori
                                if (colonna < numeroColonne - 1)
                                {
                                    colonna++;
                                }
                                else
                                {
                                    if (riga < numeroRighe - 1)
                                    {
                                        riga++;
                                        colonna = 0;
                                    }
                                    else
                                    {
                                        riga = 0;
                                        colonna = 0;
                                        strato++;
                                    }
                                }
                                /*
                                // Movimento cartesiano
                                movementResult = robot.MoveCart(descPosPlace, tool, user, vel, acc,
                                    ovl, blendT, config); // Invio punto di place
                                GetRobotMovementCode(movementResult);
                                */

                                endingPoint = puntoPlaceScatola; // Assegnazione endingPoint

                                step = 70;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 60 - Movimento a punto di place");
                            break;

                        #endregion

                        case 70:
                            #region Delay per calcolo in position punto di place

                            Thread.Sleep(500);
                            step = 80;
                            formDiagnostics.UpdateRobotStepDescription("STEP 70 -  Delay calcolo in position punto di place");
                            break;

                        #endregion

                        case 80:
                            #region Attesa inPosition punto di Place

                            if (inPosition) // Se il Robot è arrivato in posizione di place
                            {
                                // Chiudo la pinza
                              
                                step = 90;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 80 - Attesa inPosition punto di place");
                            break;
                        #endregion

                        case 90:
                            #region Rilascio focaccia

                            // if (gripperStatus == 0)
                            {
                                Thread.Sleep(500); // Per evitare "rimbalzo" del Robot
                                step = 100;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 90 - Rilascio focaccia");
                            break;

                        #endregion

                        case 100:
                            #region Movimento a punto di Home e riavvio ciclo

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosApproachPlace, descPosApproachPlace, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );
                            /*
                            // Movimento cartesiano
                            movementResult = robot.MoveCart(descPosApproachPlace, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di avvicinamento place
                            GetRobotMovementCode(movementResult);
                            */

                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);

                            formDiagnostics.UpdateRobotStepDescription("STEP 100 - Movimento a punto di Home e riavvio ciclo");

                            step = 0;
                            break;
                            #endregion

                    }

                    Thread.Sleep(10); // Delay routine
                }
            });


        }

        /// <summary>
        /// Normalizza angolo robot
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle <= -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Esegue ciclo teglie
        /// </summary>
        public static async void PickAndPlaceTegliaGiroCompleto()
        {
            #region Dichiarazione punti routine

            // Parametro necessario al comando MoveL
            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset

            int offsetAllontamento = 900;
            int offsetAvvicinamento = 200;

            #region Punto home

            JointPos jointPosHome= new JointPos(0, 0, 0, 0, 0, 0);
            var home = ApplicationConfig.applicationsManager.GetPosition("pHomeTeglia", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);
            RobotManager.robot.GetInverseKin(0, descPosHome, -1, ref jointPosHome);

            #endregion


            #region Prima fase

            #region Punto di pick

            JointPos jointPosPick = new JointPos(0, 0, 0, 0, 0, 0);
            var pick = ApplicationConfig.applicationsManager.GetPosition("pPickTeglia", "RM");
            DescPose descPosPick = new DescPose(pick.x, pick.y, pick.z, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick, -1, ref jointPosPick);

            #endregion

            #region Punto avvicinamento pick

            // Prima fase del ciclo
            JointPos jointPosApproachPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick = new DescPose(pick.x - offsetAvvicinamento, pick.y, pick.z - 40, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick, -1, ref jointPosApproachPick);

            #endregion
    
            #region Punto post pick

            JointPos jointPosPostPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPick = new DescPose(pick.x, pick.y, pick.z + 40, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPick, -1, ref jointPosPostPick);

            #endregion

            #region Punto allontanamento post pick

            JointPos jointPosAllontanamentoPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPick = new DescPose(descPosApproachPick.tran.x, descPosApproachPick.tran.y,
                descPosApproachPick.tran.z + 80, descPosApproachPick.rpy.rx, descPosApproachPick.rpy.ry, descPosApproachPick.rpy.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPick, -1, ref jointPosAllontanamentoPick);

            #endregion


            #region Punto di place

            JointPos jointPosPlace = new JointPos(0, 0, 0, 0, 0, 0);
            var place = ApplicationConfig.applicationsManager.GetPosition("pPlaceTeglia", "RM");
            DescPose descPosPlace = new DescPose(place.x, place.y, place.z, NormalizeAngle((float)place.rx + 3), place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPlace, -1, ref jointPosPlace);

            #endregion

            #region Punto avvicinamento place

            JointPos jointPosApproachPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPlace = new DescPose(place.x, place.y - offsetAvvicinamento, place.z + 20,
                place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPlace, -1, ref jointPosApproachPlace);

            #endregion

            #region Punto post place

            JointPos jointPosPostPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPlace = new DescPose(place.x, place.y, place.z,
                place.rx - 3, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPlace, -1, ref jointPosPostPlace);

            #endregion

            #region Punto allontanamento place

            JointPos jointPosAllontanamentoPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPlace = new DescPose(place.x, place.y - offsetAvvicinamento - 200, place.z,
                place.rx - 3, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPlace, -1, ref jointPosAllontanamentoPlace);

            #endregion

            #endregion

            #region Seconda fase

            #region Punto di pick 2

            JointPos jointPosPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            var pick2 = place;
            DescPose descPosPick2 = new DescPose(pick2.x, pick2.y, pick2.z, pick2.rx, pick2.ry, pick2.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick2, -1, ref jointPosPick2);

            #endregion

            #region Punto avvicinamento pick

            // Prima fase del ciclo
            JointPos jointPosApproachPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick2 = new DescPose(pick2.x, pick2.y - offsetAvvicinamento, pick2.z - 40, pick2.rx, pick2.ry, pick2.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick2, -1, ref jointPosApproachPick2);

            #endregion

            #region Punto post pick

            JointPos jointPosPostPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPick2 = new DescPose(pick2.x, pick2.y, pick2.z + 40, pick2.rx, pick2.ry, pick2.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPick2, -1, ref jointPosPostPick2);

            #endregion

            #region Punto allontanamento post pick

            JointPos jointPosAllontanamentoPick2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPick2 = new DescPose(descPosPick2.tran.x, descPosPick2.tran.y - offsetAllontamento,
                descPosPick2.tran.z + 40, descPosPick2.rpy.rx, descPosPick2.rpy.ry, descPosPick2.rpy.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPick2, -1, ref jointPosAllontanamentoPick2);

            #endregion


            #region Punto avvicinamento place

            JointPos jointPosApproachPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPlace2 = new DescPose(pick.x - offsetAvvicinamento, pick.y , pick.z + 50,
                pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPlace2, -1, ref jointPosApproachPlace2);

            #endregion

            #region Punto di place

            JointPos jointPosPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPlace2 = new DescPose(pick.x - 10, pick.y, pick.z, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPlace2, -1, ref jointPosPlace2);

            #endregion

            #region Punto post place

            JointPos jointPosPostPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPlace2 = new DescPose(pick.x, pick.y, pick.z,
                pick.rx - 3, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPlace2, -1, ref jointPosPostPlace2);

            #endregion

            #region Punto allontanamento place

            JointPos jointPosAllontanamentoPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPlace2 = new DescPose(pick.x - offsetAvvicinamento, pick.y , pick.z,
                pick.rx - 3, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPlace2, -1, ref jointPosAllontanamentoPlace2);

            #endregion

           

            #endregion

            // Posa pre pick 1
            DescPose prePick1Pose = new DescPose(
                                descPosHome.tran.x,
                                descPosHome.tran.y,
                                descPosPick.tran.z - 40, // Mi abbasso di 4 cm prima di andare in pick
                                descPosHome.rpy.rx,
                                descPosHome.rpy.ry,
                                descPosHome.rpy.rz
                                );

            JointPos jointPrePick1 = new JointPos(0, 0, 0, 0, 0, 0);
            RobotManager.robot.GetInverseKin(0, prePick1Pose, -1, ref jointPrePick1);

            // Posa post pick 1
            DescPose postPick1Pose = new DescPose(
                              descPosHome.tran.x,
                              descPosHome.tran.y,
                              descPosPick.tran.z + 20, // Mi alzo di 2 cm per uscire dal carrello senza strisciare
                              NormalizeAngle((float)(descPosPick.rpy.rx + 2)),
                              descPosHome.rpy.ry,
                              descPosHome.rpy.rz
                              );

            JointPos jointPostPick1 = new JointPos(0, 0, 0, 0, 0, 0);
            RobotManager.robot.GetInverseKin(0, postPick1Pose, -1, ref jointPostPick1);

            // Posa per ruotare il robot dopo il pick 1 e prima di andare al place 1
            DescPose postPick1RotationPose = new DescPose(
                descPosHome.tran.x, 
                descPosPlace.tran.y, 
                descPosHome.tran.z, 
                descPosPlace.rpy.rx, 
                descPosPlace.rpy.ry, 
                descPosPlace.rpy.rz
                );

            // Posa post place 1
            DescPose postPlace1Pose = new DescPose(
                descPosHome.tran.x,
                descPosPlace.tran.y,
                descPosPick.tran.z + 20, // Mi alzo di 2 cm prima di andare in home
                descPosHome.rpy.rx,
                descPosHome.rpy.ry,
                descPosPlace.rpy.rz
                );

            JointPos jointPostPlace1 = new JointPos(0, 0, 0, 0, 0, 0);
            RobotManager.robot.GetInverseKin(0, postPlace1Pose, -1, ref jointPostPlace1);

            // Posa per ruotare il robot dopo il place 1 e prima di andare al place 2
            DescPose postPlace1RotationPose = new DescPose(
                descPosHome.tran.x, 
                descPosHome.tran.y, 
                descPosHome.tran.z + 20, 
                descPosHome.rpy.rx, 
                descPosHome.rpy.ry, 
                descPosHome.rpy.rz);

            DescPose place2Pose = descPosPick;
            JointPos jointPlace2 = new JointPos(0, 0, 0, 0, 0, 0);         
            RobotManager.robot.GetInverseKin(0, place2Pose, -1, ref jointPlace2);

            // Posa per ruotare il robot dopo il place 1 e prima di andare al place 2
            DescPose postPlace2Pose = new DescPose(
                descPosHome.tran.x,
                descPosHome.tran.y,
                descPosPick.tran.z - 20, // Mi alzo di 4 cm prima di andare in home
                descPosPick.rpy.rx - 4,
                descPosHome.rpy.ry,
                descPosHome.rpy.rz
                );

            JointPos jointPostPlace2 = new JointPos(0, 0, 0, 0, 0, 0);
            RobotManager.robot.GetInverseKin(0, postPlace2Pose, -1, ref jointPostPlace2);

            #endregion

            #region Parametri movimento

            ExaxisPos epos = new ExaxisPos(0, 0, 0, 0); // Nessun asse esterno
            byte offsetFlag = 0; // Flag per offset (0 = disabilitato)

            // Indica il codice risultante del movimento del Robot
            int movementResult = -1;

            // Reset condizione di stop ciclo
            stopCycleRoutine = false;

            // Reset richiesta di stop ciclo
            stopCycleRequested = false;

            // Reset step routine
            step = 0;

            // Dichiarazione stato della pinza
            int gripperStatus = -1;

            // Segnale di pick
            bool prendidaNastro = true;

            // Segnale di place
            bool appoggiaSuScatola = true;

            #endregion

            double[] levelCollision1 = new double[] { 1, 1, 1, 1, 1, 1 };
            double[] levelCollision2 = new double[] { 2, 2, 2, 2, 2, 2 };
            double[] levelCollision3 = new double[] { 3, 3, 3, 3, 3, 3 };
            double[] levelCollision4 = new double[] { 4, 4, 4, 4, 4, 4 };
            double[] levelCollision5 = new double[] { 5, 5, 5, 5, 5, 5 };
            double[] levelCollision6 = new double[] { 6, 6, 6, 6, 6, 6 };
            double[] levelCollision7 = new double[] { 7, 7, 7, 7, 7, 7 };
            double[] levelCollision8 = new double[] { 8, 8, 8, 8, 8, 8 };

            robot.SetAnticollision(0, levelCollision6, 1);

            byte ris = 0;

            // Aspetto che il metodo termini, ma senza bloccare il thread principale
            // La routine è incapsulata come 'async' per supportare futuri operatori 'await' nel caso ci fosse la necessità
            await Task.Run(async () =>
            {
                // Fino a quando la condizione di stop routine non è true e non sono presenti allarmi bloccanti
                while (!stopCycleRoutine && !AlarmManager.blockingAlarm)
                {
                    switch (step)
                    {
                        case 0:
                            #region Check richiesta interruzione ciclo

                            if (!stopCycleRequested) // Se non è stata richiesta nessuna interruzione
                            {
                                step = 10;
                            }
                            else // Se è stata richiesta l'interruzione
                            {
                                // Ritorno del Robot a casa
                                GoToHomePosition();

                                // Reset inPosition
                                inPosition = false;

                                // Assegnazione del pHome come ending point
                                endingPoint = descPosHome;

                                step = 5;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 0 - Check richiesta interruzione ciclo");
                            break;

                        #endregion

                        case 5:
                            #region Termine routine

                            if (inPosition) // Se il Robot è arrivato in HomePosition
                            {
                                // Abilito il tasto Start per avviare nuovamente la routine
                                EnableButtonCycleEvent?.Invoke(1, EventArgs.Empty);

                                // Imposto a false il booleano che fa terminare il thread della routine
                                stopCycleRoutine = true;

                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 5 - Termine routine");
                            break;

                        #endregion

                        case 10:
                            #region Movimento a punto di Pick

                             inPosition = false; // Reset inPosition
                            
                            // Movimento in posa di pre pick 1
                            movementResult = robot.MoveL(jointPrePick1, prePick1Pose, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult); // Stampo risultato del movimento

                            // Movimento in posa di pick 1
                            movementResult = robot.MoveL(jointPosPick, descPosPick, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult); // Stampo risultato del movimento
                            
                            endingPoint = descPosPick; // Assegnazione endingPoint
                           
                            step = 30; // Passaggio a step 30
                            
                            formDiagnostics.UpdateRobotStepDescription("STEP 10 - Movimento a punto di Pick 1");

                            break;

                        #endregion

                        case 20:
                            #region Delay per calcolo in position punto di pick 1

                            await Task.Delay(500);

                            step = 30; // Passaggio a step 30

                            formDiagnostics.UpdateRobotStepDescription("STEP 20 -  Delay calcolo in position punto di pick 1");

                            break;

                        #endregion

                        case 30:
                            #region Attesa inPosition punto di Pick e chiusura pinza

                            if (inPosition) // Se il Robot è arrivato in posizione di Pick 1
                            {
                                // Chiudo la pinza
                                robot.SetDO(0, 1, 0, 0);

                                step = 40; // Passaggio a step 40
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 30 - Attesa inPosition punto di Pick 1");

                            break;

                        #endregion

                        case 40:
                            #region Delay post chiusura pinza

                            robot.GetDI(0, 1, ref ris);

                             if (ris == 0)
                            {
                               
                                //await Task.Delay(800); // Ritardo per evitare che il robot riparta senza aver finito di chiudere la pinza
                                step = 50;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 40 - Delay post chiusura pinza in fase di pick 1");

                            break;

                        #endregion

                        case 50:
                            #region Movimento di uscita dal carrello dopo pick 1

                            // Movimento per uscire dal carrelo dopo pick 1
                            movementResult = robot.MoveL(jointPostPick1, postPick1Pose, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);

                            formDiagnostics.UpdateRobotStepDescription("STEP 50 - Movimento di uscita dal carrello dopo pick 1");

                            step = 60;

                            break;

                        #endregion

                        case 60:
                            #region Movimento a punto di place 1

                            inPosition = false; // Reset inPosition

                            // Movimento di rotazione prima di andare in place 1
                            movementResult = robot.MoveCart(postPick1RotationPose, tool, user, vel/2, acc/2, ovl, blendT, config);
                            GetRobotMovementCode(movementResult);

                            // Movimento in place 1
                            movementResult = robot.MoveL(jointPosPlace, descPosPlace, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);

                            endingPoint = descPosPlace; // Assegnazione endingPoint

                            step = 80; // Passaggio a step 80
                            
                            formDiagnostics.UpdateRobotStepDescription("STEP 60 - Movimento a punto di place 1");

                            break;

                        #endregion

                        case 70:
                            #region Delay per calcolo in position punto di place

                            await Task.Delay(500);
                            step = 80; // Passaggio a step 80

                            formDiagnostics.UpdateRobotStepDescription("STEP 70 -  Delay calcolo in position punto di place (prima fase)");

                            break;

                        #endregion

                        case 80:
                            #region Attesa inPosition punto di Place 1

                            if (inPosition) // Se il Robot è arrivato in posizione di place
                            {
                                await Task.Delay(1000);
                                step = 100; // Passaggio a step 100
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 80 - Attesa inPosition punto di place 1");

                            break;

                        #endregion

                        case 90:
                            #region Rilascio teglia

                            // if (gripperStatus == 0)
                            {
                                await Task.Delay(500);
                                step = 100;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 90 - Rilascio teglia (prima fase)");
                            break;

                        #endregion

                        case 100:
                            #region Movimento per uscire da place 1 ed entrare in place 2

                            // Movimento per uscire da place 1
                            movementResult = robot.MoveL(jointPostPlace1, postPlace1Pose, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult); // Stampo risultato del movimento

                            // Movimento di preparazione al place 2
                            movementResult = robot.MoveCart(postPlace1RotationPose, tool, user, vel/2, acc/2, ovl, blendT, config);
                            GetRobotMovementCode(movementResult);

                            endingPoint = postPlace1RotationPose;

                            formDiagnostics.UpdateRobotStepDescription("STEP 100 - Movimento a punto di place 2");

                            step = 105;

                            break;

                        #endregion

                        case 105:
                            #region Attesa inPosition punto pre place 2

                            if (inPosition) // Se il Robot è arrivato in posizione di place 2
                            {
                              //  await Task.Delay(300);
                               // robot.SetAnticollision(0, levelCollision5, 1);

                                // Movimento a posa di place 2
                                movementResult = robot.MoveL(jointPlace2, place2Pose, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);

                                endingPoint = place2Pose; // Assegnazione ending point

                                formDiagnostics.UpdateRobotStepDescription("STEP 100 - Movimento a punto di place 2");

                                step = 130; // Passaggio a step 140
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 130 - Attesa inPosition punto pre place");

                            break;

                            #endregion

                        case 120:
                            #region Delay per calcolo in position punto di place 2

                            await Task.Delay(500);
                            step = 130; 

                            formDiagnostics.UpdateRobotStepDescription("STEP 120 -  Delay calcolo in position punto di place 2");
                            break;

                        #endregion

                        case 130:
                            #region Attesa inPosition punto di place 2

                            if (inPosition) // Se il Robot è arrivato in posizione di place 2
                            {
                                robot.SetDO(0, 0, 0, 0); // Apro la pinza

                                step = 140; // Passaggio a step 140
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 130 - Attesa inPosition punto di place 2 e apertura pinza");

                            break;

                        #endregion

                        case 140:
                            #region Delay post apertura pinza

                            robot.GetDI(0, 1, ref ris);

                            if (ris == 1)
                            {
                              // await Task.Delay(300); // // Ritardo per evitare che il robot riparta senza aver finito di aprire la pinza
                              //  robot.SetAnticollision(0, levelCollision5, 1);

                                step = 150; // Passaggio a step 150
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 140 - Delay post apertura pinza in place 2");

                            break;

                        #endregion

                        case 150:
                            #region Movimento per uscire dal carrello dopo il place 2

                            // Movimento per uscire dal carrello dopo il place 2
                            movementResult = robot.MoveL(jointPostPlace2, postPlace2Pose, tool, user, vel, acc, ovl, blendT, epos, 0, offsetFlag, offset);

                            formDiagnostics.UpdateRobotStepDescription("STEP 150 - Movimento per uscire dal carrello dopo il place 2");

                            await Task.Delay(500); // Delay prima di riavviare il ciclo
                            step = 0;
                            break;
                        #endregion

                        case 160:
                            #region Movimento a punto di Place

                            if (prendidaNastro)
                            {
                                inPosition = false; // Reset inPosition

                                // STEP 3: Invio punto di avvicinamento Pick
                               /* movementResult = robot.MoveCart(descPosApproachPlace2, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);*/

                                // STEP 4: Movimento lineare finale
                                movementResult = robot.MoveL(jointPosApproachPlace2, descPosApproachPlace2, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset);
                                GetRobotMovementCode(movementResult);

                                // STEP 4: Movimento lineare finale
                                movementResult = robot.MoveL(jointPosPlace2, descPosPlace2, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset);
                                GetRobotMovementCode(movementResult);

                                endingPoint = descPosPlace2; // Assegnazione endingPoint
                                step = 170;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 160 - Movimento a punto di Place (seconda fase)");
                            break;
                        #endregion


                        case 170:
                            #region Delay per calcolo in position punto di place

                           // Thread.Sleep(500);
                            step = 180;
                            formDiagnostics.UpdateRobotStepDescription("STEP 170 -  Delay calcolo in position punto di place (seconda fase)");
                            break;

                        #endregion

                        case 180:
                            #region Attesa inPosition punto di Place

                            if (inPosition) // Se il Robot è arrivato in posizione di Pick
                            {
                               // Thread.Sleep(500);
                                // Chiudo la pinza
                              
                                step = 190;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 180 - Attesa inPosition punto di place (seconda fase)");
                            break;

                        #endregion

                        case 190:
                            #region Rilascio teglia

                            // if (gripperStatus == 0)
                            {
                                Thread.Sleep(500); // Per evitare "rimbalzo" del Robot
                                step = 200;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 190 - Rilascio teglia (seconda fase)");
                            break;

                        #endregion

                        case 200:
                            #region Movimento a punto di Home e riavvio ciclo

                            movementResult = robot.MoveL(jointPosPostPlace2, descPosPostPlace2, tool, user, vel, acc,
                                   ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult);

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosAllontanamentoPlace2, descPosAllontanamentoPlace2, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            /*
                            movementResult = robot.MoveCart(descPosHome, tool, user, vel, acc,
                                ovl, blendT, config); // Invio punto di Home
                            GetRobotMovementCode(movementResult);
                            */

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosHome, descPosHome, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );

                            formDiagnostics.UpdateRobotStepDescription("STEP 200 - Movimento a punto di Home e riavvio ciclo");

                            step = 0;
                            break;

                            #endregion
                    }

                    Thread.Sleep(50); // Delay routine
                }
            });


        }

        /// <summary>
        /// Calcola la  pose in cui mettere la scatola nel pallet. <br/>
        /// Il calcolo si basa sulla divisione del pallet (messo per il lato lungo in basso) in una matrice di place in questa maniera: <br/>
        /// /  (Larghezza) = Divisione in colonne <br/>
        /// -- (Lunghezza) = Divisione in righe <br/>
        /// I  (Altezza)   = Divisione in strati <br/>
        /// Inoltre sono necessiarie le misure Larg,Lung,Alt della scatola <br/>
        /// NB: le scatole devono essere necessariamente tutte grandi uguali nello stesso pallet <br/>
        /// </summary>
        /// <param name="riga">Riga che identifica il posto in cui mettere la nuova scatola</param>
        /// <param name="colonna">Colonna che identifica il posto in cui mettere la nuova scatola</param>
        /// <param name="strato">Strato in cui mettere la nuova scatola</param>
        /// <param name="larghezzaScatola">Larghezza della scatola(mm)</param>
        /// <param name="profonditaScatola">Lunghezza della scatola(mm)</param>
        /// <param name="altezzaScatola">Altezza della scatola(mm)</param>
        /// <param name="originePallet">Punto di origine del pallet, cioè il punto dell'angolo in alto a sinistra. Posizionare il TCP proprio sopra di esso.</param>
        /// <returns></returns>
        static DescPose CalcolaPosizioneScatola(
            int riga,
            int colonna,
            int strato,
            double larghezzaScatola,
            double profonditaScatola,
            double altezzaScatola,
            DescPose originePallet
            )
        {
            double x = originePallet.tran.x + (colonna * larghezzaScatola) + (larghezzaScatola / 2.0);
            double y = originePallet.tran.y + (riga * profonditaScatola) + (profonditaScatola / 2.0);
            double z = originePallet.tran.z + (strato * altezzaScatola);

            double rx = originePallet.rpy.rx;
            double ry = originePallet.rpy.ry;
            double rz = originePallet.rpy.rz;

            return new DescPose(x, y, z, rx, ry, rz);
        }

        public static int robotEnableStatus = 0;
      
        /// <summary>
        /// Esegue check su Robot enable
        /// </summary>
        public static void CheckIsRobotEnable()
        {
            // Controllo se il robot è abilitato tramite PLC
            isEnabledNow = Convert.ToBoolean(PLCConfig.appVariables.getValue(PLCTagName.Enable));

            if (isEnabledNow && !prevIsEnable)
            {
                // Abilitazione del robot
                int err = robot.RobotEnable(1);
                prevIsEnable = true;
                prevIsNotEnable = false; // Resetta lo stato "non abilitato"
                AlarmManager.blockingAlarm = false;
                robotEnableStatus = 1;
                currentIndex = -1;
            }
            else if (!isEnabledNow && !prevIsNotEnable)
            {
                // Disabilitazione del robot
                JogMovement.StopJogRobotThread(); // Ferma il thread di Jog
                robot.RobotEnable(0);
                prevIsNotEnable = true;
                prevIsEnable = false; // Resetta lo stato "abilitato"
                prevIsManual = false;
                AlarmManager.blockingAlarm = true;
                pauseCycleRequested = false;
                currentIndex = -1;
                robotEnableStatus = 0;
                UC_FullDragModePage.debugCurrentIndex = -1;
                robot.StopMotion(); // Cancellazione della coda di punti
            }  
        }

        /// <summary>
        /// Invia posizioni al PLC in formato cartesiano e joint
        /// </summary>
        /// <param name="jPos">Posizione in joint ottenuta dal calcolo di cinematica inversa partendo dalla posizione TCP</param>
        /// <param name="j1_actual_pos">Posizione del giunto 1</param>
        /// <param name="j2_actual_pos">Posizione del giunto 2</param>
        /// <param name="j3_actual_pos">Posizione del giunto 3</param>
        /// <param name="j4_actual_pos">Posizione del giunto 4</param>
        /// <param name="j5_actual_pos">Posizione del giunto 5</param>
        /// <param name="j6_actual_pos">Posizione del giunto 6</param>
        public static void CheckRobotPosition(
            JointPos jPos, 
            JointPos j1_actual_pos, 
            JointPos j2_actual_pos, 
            JointPos j3_actual_pos,
            JointPos j4_actual_pos, 
            JointPos j5_actual_pos, 
            JointPos j6_actual_pos
            )
        {
            // Calcolo della posizione in joint eseguendo il calcolo di cinematica inversa
            robot.GetInverseKin(0, TCPCurrentPosition, -1, ref jPos);

            #region TCP

            // Scrittura posizione su asse x
            RefresherTask.AddUpdate(PLCTagName.x_actual_pos, TCPCurrentPosition.tran.x, "FLOAT");

            // Scrittura posizione su asse y
            RefresherTask.AddUpdate(PLCTagName.y_actual_pos, TCPCurrentPosition.tran.y, "FLOAT");

            // Scrittura posizione su asse z
            RefresherTask.AddUpdate(PLCTagName.z_actual_pos, TCPCurrentPosition.tran.z, "FLOAT");

            // Scrittura posizione su asse rx
            RefresherTask.AddUpdate(PLCTagName.rx_actual_pos, TCPCurrentPosition.rpy.rx, "FLOAT");

            // Scrittura posizione su asse ry
            RefresherTask.AddUpdate(PLCTagName.ry_actual_pos, TCPCurrentPosition.rpy.ry, "FLOAT");

            // Scrittura posizione su asse rz
            RefresherTask.AddUpdate(PLCTagName.rz_actual_pos, TCPCurrentPosition.rpy.rz, "FLOAT");

            #endregion

            #region Joint

            // Scrittura posizione giunto 1
            RefresherTask.AddUpdate(PLCTagName.j1_actual_pos, jPos.jPos[0], "FLOAT");

            // Scrittura posizione giunto 2
            RefresherTask.AddUpdate(PLCTagName.j2_actual_pos, jPos.jPos[1], "FLOAT");

            // Scrittura posizione giunto 3
            RefresherTask.AddUpdate(PLCTagName.j3_actual_pos, jPos.jPos[2], "FLOAT");

            // Scrittura posizione giunto 4
            RefresherTask.AddUpdate(PLCTagName.j4_actual_pos, jPos.jPos[3], "FLOAT");

            // Scrittura posizione giunto 5
            RefresherTask.AddUpdate(PLCTagName.j5_actual_pos, jPos.jPos[4], "FLOAT");

            // Scrittura posizione giunto 6
            RefresherTask.AddUpdate(PLCTagName.j6_actual_pos, jPos.jPos[5], "FLOAT");

            #endregion
        }

        static DateTime? robotMovingStartTime = null;

        /// <summary>
        /// Check su movimento del Robot
        /// </summary>
        /// <param name="updates"></param>
        public static void CheckIsRobotMoving(List<(string key, bool value, string type)> updates)
        {

            if (AlarmManager.isRobotConnected)
            {
                double[] coordNewTCPposition = {
                    Math.Round(TCPCurrentPosition.tran.x, 0),
                    Math.Round(TCPCurrentPosition.tran.y, 0),
                    Math.Round(TCPCurrentPosition.tran.z, 0),
                    Math.Round(TCPCurrentPosition.rpy.rx, 0),
                    Math.Round(TCPCurrentPosition.rpy.ry, 0),
                    Math.Round(TCPCurrentPosition.rpy.rz, 0)
                };

                double[] coordpreviousTCPposition = {
                    Math.Round(previousTCPposition.tran.x, 0),
                    Math.Round(previousTCPposition.tran.y, 0),
                    Math.Round(previousTCPposition.tran.z, 0),
                    Math.Round(previousTCPposition.rpy.rx, 0),
                    Math.Round(previousTCPposition.rpy.ry, 0),
                    Math.Round(previousTCPposition.rpy.rz, 0)
                };

                //TODO: è possibile aggiungere una tolleranza per ridurre ancora la quantità di allarmi generati

                // Confronta gli array arrotondati
                bool sonoUguali = coordNewTCPposition.SequenceEqual(coordpreviousTCPposition);

                if (sonoUguali)
                {
                    if (AlarmManager.isRobotMoving)
                    {
                        AlarmManager.isRobotMoving = false;
                        RobotIsMoving?.Invoke(false, EventArgs.Empty);
                        robotMovingStartTime = null; // Resetta il timer
                    }
                }
                else
                {
                    if (!AlarmManager.isRobotMoving)
                    {
                        // Quando il robot inizia a muoversi, avvia il timer
                        if (robotMovingStartTime == null)
                        {
                            robotMovingStartTime = DateTime.Now;
                        }
                        else if ((DateTime.Now - robotMovingStartTime.Value).TotalSeconds > 2)
                        {
                            // Invoca l'evento solo dopo 1 secondo
                            AlarmManager.isRobotMoving = true;
                            RobotIsMoving?.Invoke(true, EventArgs.Empty);
                            robotMovingStartTime = null; // Resetta il timer dopo l'invocazione
                        }
                    }
                    else
                    {
                        robotMovingStartTime = null; // Resetta il timer se torna falso
                    }
                }
            }

            // Aggiorna la posizione TCP precedente con la posizione TCP attuale
            previousTCPposition.tran.x = TCPCurrentPosition.tran.x;
            previousTCPposition.tran.y = TCPCurrentPosition.tran.y;
            previousTCPposition.tran.z = TCPCurrentPosition.tran.z;
            previousTCPposition.rpy.rx = TCPCurrentPosition.rpy.rx;
            previousTCPposition.rpy.ry = TCPCurrentPosition.rpy.ry;
            previousTCPposition.rpy.rz = TCPCurrentPosition.rpy.rz;

        }

        /// <summary>
        /// Approssima i valori delle posizioni a n cifre decimali
        /// </summary>
        /// <param name="dp">Contiene il riferimento allo struct che contiene i valori da approssimare</param>
        /// <param name="digits">Numero di cifre decimali desiderate</param>
        private static void RoundPositionDecimals(ref DescPose dp, int digits)
        {
            dp.tran.x = Math.Round(dp.tran.x, digits);
            dp.tran.y = Math.Round(dp.tran.y, digits);
            dp.tran.z = Math.Round(dp.tran.z, digits);
            dp.rpy.rx = Math.Round(dp.rpy.rx, digits);
            dp.rpy.ry = Math.Round(dp.rpy.ry, digits);
            dp.rpy.rz = Math.Round(dp.rpy.rz, digits);
        }

        /// <summary>
        /// Metodo che ferma il robot e cancella la coda di punti
        /// </summary>
        public static void ClearRobotQueue()
        {
            AlarmManager.isRobotMoving = false;
            robot.PauseMotion();
            robot.StopMotion();
        }

        /// <summary>
        /// Creazione di un allarme quando il robot si ferma
        /// </summary>
        /// <param name="id">ID allarme</param>
        /// <param name="description">Descrizione allarme</param>
        /// <param name="timestamp">Timestamp allarme</param>
        /// <param name="device">Device da cui deriva l'allarme</param>
        /// <param name="state">ON-OFF</param>
        public static void CreateRobotAlarm(string id, string description, string timestamp, string device, string state)
        {
            // Solleva l'evento quando il robot si ferma
            OnRobotAlarm(new RobotAlarmsEventArgs(id, description, timestamp, device, state));
        }

        /// <summary>
        /// Metodo che aggiunge alla lista degli allarmi l'allarme
        /// </summary>
        /// <param name="e"></param>
        public static void OnRobotAlarm(RobotAlarmsEventArgs e)
        {
            // Calcola il timestamp Unix in millisecondi
            long unixTimestamp = ((DateTimeOffset)Convert.ToDateTime(e.Timestamp)).ToUnixTimeMilliseconds();

            RobotDAO.SaveRobotAlarm(ConnectionString, Convert.ToInt32(e.Id), e.Description,
                unixTimestamp.ToString(), e.Device, e.State);
            formAlarmPage.AddAlarmToList(e.Id, e.Description, e.Timestamp, e.Device, e.State);
            TriggerAllarmeGenerato();

        }

        /// <summary>
        /// Metodo che mette in pausa il Robot
        /// </summary>
        public static void PauseMotion()
        {
            robot.PauseMotion();
            log.Warn("Il robot è stato messo in pausa");
        }

        /// <summary>
        /// Metodo che riprende movimento Robot
        /// </summary>
        public static void ResumeMotion()
        {
            robot.ResumeMotion();
        }

        /// <summary>
        /// Metodo che porta il Robot in HomePosition
        /// </summary>
        public static void GoToHomePosition()
        {
            var restPose = ApplicationConfig.applicationsManager.GetPosition("1", "RM");
            DescPose pHome = new DescPose(restPose.x, restPose.y, restPose.z, restPose.rx, restPose.ry, restPose.rz);
     

            // test RE #####################################################################################
            DescPose RE_tool_1 = new DescPose(0, 0, 50, 0, 0, 0);
            robot.SetToolCoord(1, RE_tool_1,0,0);
            robot.GetActualTCPNum(0, ref tool);

            DescPose RE_frame_1 = new DescPose(-830.117, 207.966, -620.278, 0.006, 0.009, -147.102);
            robot.SetWObjCoord(1,RE_frame_1);
            robot.GetActualWObjNum(0,ref user);
            // test RE #####################################################################################


            int result = robot.MoveCart(pHome, tool, user, vel, acc, ovl, blendT, config);

            GetRobotMovementCode(result);
        }

        /// <summary>
        /// Imposta la velocità predefinita per eseguire la home routine
        /// </summary>
        public static void SetHomeRoutineSpeed()
        {
            robot.SetSpeed(homeRoutineSpeed);
            vel = homeRoutineVel;
            acc = homeRoutineAcc;
        }

        /// <summary>
        /// Resetta la velocità utilizzata per la home routine
        /// </summary>
        public static void ResetHomeRoutineSpeed()
        {
            robot.SetSpeed(robotProperties.Speed);
            vel = robotProperties.Velocity;
            acc = robotProperties.Acceleration;
        }

        /// <summary>
        /// Porta il Robot in posizione sicura prima di tornare in Home
        /// </summary>
        public static void MoveRobotToSafePosition()
        {
            if (isInPositionPick)
            {
                RetractRobotToSafePosition(200.0f); // Mi alzo dal punto in cui mi trovo di 200mm
            }
            else
            if (isInPositionPlace)
            {
                RetractRobotToSafePosition(200.0f); // Mi alzo dal punto in cui mi trovo di 200mm
            }
            if (isInPositionWelding)
            {
                RetractRobotBackward(110.0f); //-119.0f 
            }
        }
        /// <summary>
        /// Metodo per reset errori Robot
        /// </summary>
        public static void ClearRobotAlarm()
        {
            int err = robot.ResetAllError();
        }

        /// <summary>
        /// Imposta la modalità operativa del robot: 
        /// <para>0 = automatico</para>
        /// <para>1 = manuale</para>
        /// </summary>
        /// <param name="mode"></param>
        public static void SetRobotMode(int mode)
        {
            if (mode != 0 && mode != 1)
                return;


            //if(isAutomaticMode && mode == 1) 
            //  RequestStopCycle();

            robot.Mode(mode);
            isAutomaticMode = mode == 0;
        }


        #endregion

    }
}
