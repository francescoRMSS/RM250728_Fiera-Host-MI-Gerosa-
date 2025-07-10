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
using System.Reflection;
using System.Windows.Forms;
using CookComputing.XmlRpc;
using System.Web;
using System.Drawing.Imaging;

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
        /// A true se completata la pick routine
        /// </summary>
        public static bool pickRoutineDone = false;

        /// <summary>
        /// A true se completata la place routine
        /// </summary>
        public static bool placeRoutineDone = false;

        /// <summary>
        /// Mostra quali teglie sono il lavorazione
        /// </summary>
        public static bool[] teglieInLavorazione = new bool[] { false, false };

        /// <summary>
        /// True quando può partire la HomeRoutine
        /// </summary>
        public static bool startHomeRoutine = false;

        /// <summary>
        /// True quando il ciclo puo essere avviato
        /// </summary>
        public static bool startCycle = false;

        /// <summary>
        /// Identifica la posizione all'interno della teglia
        /// </summary>
        public static int trayPosition = 0;

        /// <summary>
        /// Identifica numero della teglia
        /// </summary>
        public static int trayId = 1;

        /// <summary>
        /// Limite di posizioni su asse X
        /// </summary>
        public static int xLimit = 2;

        /// <summary>
        /// Limite di posizioni su asse Y
        /// </summary>
        public static int yLimit = 2;

        /// <summary>
        /// Posizione corrente su asse X
        /// </summary>
        public static int[] xCurrent = new int[] { 0, 0 };

        /// <summary>
        /// Posizione corrente su asse Y
        /// </summary>
        public static int[] yCurrent = new int[] { 0, 0 };

        /// <summary>
        /// Traccia se una teglia è piena
        /// </summary>
        public static bool[] tegliaFull = new bool[] { false, false };

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
        private static PositionChecker checker_ingombro_welding;
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
        private static int lowPriorityRefreshPeriod = 100;

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
        /// Evento invocato al termine della routine per riabilitare i tasti per riavvio della routine
        /// </summary>
        public static event EventHandler EnableButtonCycleEvent;

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

        /// <summary>
        /// Variabile per memorizzare lo stato precedente di isInHomePosition
        /// </summary>
        private static bool? previousIsInHomePosition = null;

        private static bool? previousGripperStatus = null;

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
        private static void GetPLCErrorCode(Dictionary<string, object> alarmValues, Dictionary<string, string> alarmDescriptions,
        DateTime now, long unixTimestamp, DateTime dateTime, string formattedDate)
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
        /// Genera ID allarme
        /// </summary>
        /// <param name="alarmKey"></param>
        /// <returns></returns>
        private static string GenerateAlarmId(string alarmKey)
        {
            // Genera un ID univoco basato sulla chiave dell'allarme
            return alarmKey.GetHashCode().ToString();
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
        private static void GetRobotErrorCode(List<(string key, bool value, string type)> updates, bool allarmeSegnalato,
            DataRow robotAlarm, DateTime now, string id, string description, string timestamp, string device, string state, long unixTimestamp,
            DateTime dateTime, string formattedDate)
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

                    // Alzo bit per segnalare errore
                    RefresherTask.AddUpdate(PLCTagName.System_error, 1, "INT16");

                    // Segnalo che è presente un allarme bloccante (allarme robot)
                    AlarmManager.blockingAlarm = true;
                }
                else if (maincode == 0)
                {
                    // Reimposta la variabile di stato se l'allarme è risolto
                    allarmeSegnalato = false;
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

            // Collegamento evento ValueChanged del dizionario al metodo HandleDictionaryChange
            PLCConfig.appVariables.ValueChanged += RefreshVariables;

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


            // InitRobotComponents();

            // Se fallisce setting della proprietà del Robot
            if (!SetRobotProperties())
                return false;

            robot.SetSpeed(robotProperties.Speed);

            return true;
        }

        /// <summary>
        /// Gestisce gli ausiliari del Robot
        /// </summary>
        private static void AuxiliaryWorker()
        {
            // Get del punto di home dal dizionario delle posizioni
            var pHome = ApplicationConfig.applicationsManager.GetPosition("pHome", "RM");
            // Creazione della DescPose del punto di Home
            DescPose homePose = new DescPose(pHome.x, pHome.y, pHome.z, pHome.rx, pHome.ry, pHome.rz);

            while (true)
            {
                CheckIsRobotEnable();
                CheckRobotMode();
                CheckIsRobotReadyToStart();
                CheckRobotHasProgramInMemory();
                CheckIsRobotInHomePosition(homePose);
                CheckGripperStatus();

                Thread.Sleep(auxiliaryThreadRefreshPeriod);
            }
        }

        private static void CheckGripperStatus()
        {
            int gripperStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.GripperStatusIn));
            //if(stableMode == 2) 
            if(previousGripperStatus == null || Convert.ToBoolean(gripperStatus) != previousGripperStatus)
            {
                if (gripperStatus == 1)
                {
                    GripperStatusOFF?.Invoke(null, EventArgs.Empty);
                }
                else
                {
                    GripperStatusON?.Invoke(null, EventArgs.Empty);
                }

                previousGripperStatus = gripperStatus > 0;
            }
        }

        public static event EventHandler RobotInHomePosition;
        public static event EventHandler RobotNotInHomePosition;
        public static event EventHandler GripperStatusON;
        public static event EventHandler GripperStatusOFF;

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
                RefresherTask.AddUpdate(PLCTagName.Home_Pos, isInHomePosition ? 1 : 0, "INT16");

                // Aggiorna lo stato precedente
                previousIsInHomePosition = isInHomePosition;
            }
        }


        /// <summary>
        /// Stato precedente di robotReadyToStart
        /// </summary>
        private static bool prevRobotReadyToStart = false;

        /// <summary>
        /// Stato precedente di robotReadyToStart
        /// </summary>
        private static bool prevRobotHasProgramInMemory = false;

        /// <summary>
        /// Controlla che la lista di applicazione del robot abbia almeno un elemento
        /// </summary>
        private static void CheckIsRobotReadyToStart()
        {
            int numRobotApplications = ApplicationConfig.applicationsManager.getDictionary().Count;

            if (numRobotApplications > 1) // 1 perché escludo l'applicazione "RM" 
            {
                if (!prevRobotReadyToStart)
                {
                    // Imposta "Ready to Start" su 1 (true)
                    RefresherTask.AddUpdate(PLCTagName.Hardware_Ready_To_Start, 1, "INT16");
                    prevRobotReadyToStart = true; // Aggiorna lo stato
                }
            }
            else
            {
                if (numRobotApplications < 2) // 2 perché escludo l'applicazione "RM"
                {
                    // Imposta "Ready to Start" su 0 (false)
                    RefresherTask.AddUpdate(PLCTagName.Hardware_Ready_To_Start, 0, "INT16");
                    prevRobotReadyToStart = false; // Aggiorna lo stato
                }
            }
        }

        /// <summary>
        /// Controlla che il Robot abbia un'applicazione caricata in memoria
        /// </summary>
        private static void CheckRobotHasProgramInMemory()
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                if (!prevRobotHasProgramInMemory)
                {
                    // Imposta "Program_In_Memory" su 1 (true)
                    RefresherTask.AddUpdate(PLCTagName.Program_In_Memory, 0, "INT16");
                    prevRobotHasProgramInMemory = true; // Aggiorna lo stato
                }
            }
            else
            if (!string.IsNullOrEmpty(applicationName))
            {
                if (prevRobotHasProgramInMemory)
                {
                    // Imposta "Program_In_Memory" su 0 (false)
                    RefresherTask.AddUpdate(PLCTagName.Program_In_Memory, 1, "INT16");
                    prevRobotHasProgramInMemory = false; // Aggiorna lo stato
                }
            }
        }

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
        /// Inizializza tutti gli elementi del robot all'avvio:
        /// <para>Teglie</para>
        /// </summary>
        private static void InitRobotComponents()
        {
            //preparazione delle teglie
            bool[] teglia1Status = new bool[4];
            bool[] teglia2Status = new bool[4];

            int x1, x2, y1, y2;

            //teglia 1
            x1 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia1_cannolo_1"));
            x2 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia1_cannolo_2"));
            y1 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia1_cannolo_3"));
            y2 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia1_cannolo_4"));
            xCurrent[0] = x1 + x2;
            yCurrent[0] = y1 + y2;

            //teglia 2
            x1 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia2_cannolo_1"));
            x2 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia2_cannolo_2"));
            y1 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia2_cannolo_3"));
            y2 = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "Teglia2_cannolo_4"));
            xCurrent[1] = x1 + x2;
            yCurrent[1] = y1 + y2;
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
            var pickPose = ApplicationConfig.applicationsManager.GetPosition("pPick","RM");
            var placePose = ApplicationConfig.applicationsManager.GetPosition("pPlace","RM");
            var weldingPose = ApplicationConfig.applicationsManager.GetPosition("pWelding","RM");

            DescPose[] startPoints = new DescPose[]
            {
                new DescPose(pickPose.x, pickPose.y, pickPose.z, pickPose.rx, pickPose.ry, pickPose.rz),
                new DescPose(placePose.x, placePose.y, placePose.z, placePose.rx, placePose.ry, placePose.rz),
                 new DescPose(weldingPose.x, weldingPose.y, weldingPose.z, weldingPose.rx, weldingPose.ry, weldingPose.rz),
            };

            // Oggetto che rileva ingombro pick
            double delta_ingombro_pick = 100.0;
            checker_ingombro_pick = new PositionChecker(delta_ingombro_pick);

            // Oggetto che rileva ingombro place
            double delta_ingombro_place = 100.0;
            checker_ingombro_place = new PositionChecker(delta_ingombro_place);

            // Oggetto che rileva ingombro welding
            double delta_ingombro_welding = 200.0;
            checker_ingombro_welding = new PositionChecker(delta_ingombro_welding);

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

            checker_pos = new PositionChecker(2.0); // 1cm  //10.0

            while (true)
            {
                if (robot != null && AlarmManager.isRobotConnected)
                {
                    try
                    {
                        robot.GetActualTCPPose(flag, ref TCPCurrentPosition); // Leggo posizione robot TCP corrente
                        CheckIsRobotMoving(updates);
                        CheckIsRobotInObstructionArea(startPoints, updates);
                        //CheckIsRobotInSafeZone(pointSafeZone, updates);
                        CheckIsRobotInPos();

                        // SendUpdatesToPLC(updates);
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

        public static bool continueCycle = false;
        public static bool placeOnTape = false;
        public static bool goToTeglia2 = false;
        public static bool zucchera = false;



        /// <summary>
        /// Esegue reset del contatore degli step delle routine
        /// </summary>
        public static void ResetRobotSteps()
        {
            step = 0;
            pickRoutineDone = false;
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

                Thread.Sleep(lowPriorityRefreshPeriod);
            }
        }

        private static bool prevIsPlcConnected = true;
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
                    // RefresherTask.AddUpdate("PLC1_" + "CMD_Stop_Ciclo", true, "BOOL");

                    RobotManager.stopRoutine = true;
                    RobotManager.StopApplication();
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
        /// Invia aggiornamenti alla coda che esegue aggiornamento su PLC
        /// </summary>
        /// <param name="updates">Lista di aggiornamenti</param>
        private static void SendUpdatesToPLC(List<(string key, int? value, string type)> updates)
        {
            foreach (var update in updates)
            {
                RefresherTask.AddUpdate(update.key, update.value, update.type);
            }

            updates.Clear(); // Pulizia della coda
        }

        /// <summary>
        /// Verifica se il punto corrente è all'interno dell'area di ingombro rispetto a uno qualsiasi dei punti di partenza
        /// </summary>
        /// <param name="updates">Lista di aggiornamenti</param>
        private static void CheckIsRobotInObstructionArea(DescPose[] startPoints, List<(string key, bool value, string type)> updates)
        {
            isInPositionPick = checker_ingombro_pick.IsInObstruction(startPoints[0], TCPCurrentPosition);

            if (isInPositionPick)
            {
                if (prevRobotOutPick != false)
                {
                    prevRobotOutPick = false;
                    prevFuoriIngombro = false;
                }
            }

            isInPositionPlace = checker_ingombro_place.IsInObstruction(startPoints[1], TCPCurrentPosition);

            if (isInPositionPlace)
            {
                if (prevRobotOutPlace != false)
                {
                    prevRobotOutPlace = false;
                    prevFuoriIngombro = false;
                }
            }

            isInPositionWelding = checker_ingombro_welding.IsInObstruction(startPoints[2], TCPCurrentPosition);

            if (isInPositionWelding)
            {
                if (prevRobotOutWelding != false)
                {
                    prevRobotOutWelding = false;
                    prevFuoriIngombro = false;
                }
            }

            if (!isInPositionWelding && !isInPositionPick && !isInPositionWelding)
            {
                if (prevFuoriIngombro != true)
                {
                    prevFuoriIngombro = true;
                }
            }

            /*
            log.Info("\nIngombro Pick: " + isInPositionPick.ToString() +
                "\nIngombro Place: " + isInPositionPlace.ToString() + 
                "\nIngombro Welding: " + isInPositionWelding.ToString() +
                "\nFuori ingombro: " + prevFuoriIngombro.ToString()
                );
            */
        }

        /// <summary>
        /// Verifica se il punto corrente è all'interno dell'area di safe zone
        /// </summary>
        /// <param name="updates">Lista di aggiornamenti</param>
        private static void CheckIsRobotInSafeZone(DescPose pSafeZone, List<(string key, bool value, string type)> updates)
        {
            isInSafeZone = checker_safeZone.IsYLessThan(pSafeZone, TCPCurrentPosition);

            if (!AlarmManager.isFormReady)
                return;

            if (!isInSafeZone && prevIsInSafeZone != false) // Se il robot non è nella safe zone
            {
                prevIsInSafeZone = false;
                FormHomePage.Instance.RobotSafeZone.BackgroundImage = Resources.safeZone_yellow32;
                RefresherTask.AddUpdate(PLCTagName.SafePos, 0, "INT16");
            }
            else if (isInSafeZone && prevIsInSafeZone != true) // Se il robot è nella safe zone
            {
                prevIsInSafeZone = true;
                FormHomePage.Instance.RobotSafeZone.BackgroundImage = Resources.safeZone_green32;
                RefresherTask.AddUpdate(PLCTagName.SafePos, 1, "INT16");
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
        /// Metodo richiamato dall'evento ValueChanged del dizionario delle variabili PLC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void RefreshVariables(object sender, DictionaryChangedEventArgs e)
        {
            if (AlarmManager.isPlcConnected)
            {
                switch (e.Key)
                {
                    case "PLC1_" + "bStartCycle":
                        string application = RobotManager.applicationName;
                        if (e.NewValue.ToString() == "True")
                        {
                            StartApplication(application);
                        }
                        else
                        {
                            StopApplication();
                        }
                        break;
                    // Se bResetTeglia1 a true indico che la teglia 1 è libera
                    case "PLC1_" + "bResetTeglia1":
                        if (e.NewValue.ToString() == "True")
                        {
                            tegliaFull[0] = false;
                            xCurrent[0] = 0;
                            yCurrent[0] = 0;
                            RefresherTask.AddUpdate("PLC1_" + "bPlate1_Full", false, "BOOL");
                            contTrays[0] = 0;
                            RefresherTask.AddUpdate("PLC1_" + "iNrPosTeglia1", 0, "INT16");
                        }
                        break;

                    // Se bResetTeglia2 a true indico che la teglia 2 è libera
                    case "PLC1_" + "bResetTeglia2":
                        if (e.NewValue.ToString() == "True")
                        {
                            tegliaFull[1] = false;
                            xCurrent[1] = 0;
                            yCurrent[1] = 0;
                            RefresherTask.AddUpdate("PLC1_" + "bPlate2_Full", false, "BOOL");
                            contTrays[1] = 0;
                            RefresherTask.AddUpdate("PLC1_" + "iNrPosTeglia2", 0, "INT16");
                        }
                        break;

                    case "PLC1_" + "FBK Gripper Close 1":
                        if (e.NewValue.ToString() == "True")
                        {
                            gripperClosed = true;
                            gripperOpened = false;
                        }
                        else
                        {
                            gripperClosed = false;
                        }
                        break;

                    case "PLC1_" + "FBK Gripper Open":
                        if (e.NewValue.ToString() == "True")
                        {
                            gripperOpened = true;
                            gripperClosed = false;
                        }
                        else
                        {
                            gripperOpened = false;
                        }
                        break;

                    case "PLC1_" + "biBarrierOK":
                        if (e.NewValue.ToString() == "False")
                        {/*
                            if (!robotInPause)
                            {
                                robot.PauseMotion();
                                robotInPause = true;
                            }
                            */
                            velocityOverride = Convert.ToInt16(PLCConfig.appVariables.getValue("PLC1_" + "speedRobot"));

                            RefresherTask.AddUpdate("PLC1_" + "iOverride", 10, "INT16");

                            // Modifico proprietà velocity dell'oggetto robotProperties
                            RobotManager.robotProperties.Velocity = 10;

                            // Modifico la variabile globale
                            RobotManager.vel = 10;
                        }
                        if (e.NewValue.ToString() == "True")
                        {/*
                            robot.ResumeMotion();
                            robotInPause = false;
                            */

                            RefresherTask.AddUpdate("PLC1_" + "iOverride", velocityOverride, "INT16");

                            // Modifico proprietà velocity dell'oggetto robotProperties
                            RobotManager.robotProperties.Velocity = velocityOverride;

                            // Modifico la variabile globale
                            RobotManager.vel = velocityOverride;
                        }

                        break;

                        /*
                    case PLCTagName.Enable:

                        
                        if (Convert.ToBoolean(e.NewValue) == true)
                        {
                            robot.RobotEnable(1);
                        }

                        if (Convert.ToBoolean(e.NewValue) == true && !isAutomaticMode)
                            JogMovement.StartJogRobotThread();
                        
                        break;


                    case PLCTagName.Operating_Mode:

                        if (Convert.ToInt16(e.NewValue) == 0) // Off
                        {
                            robotCycleStopRequested = true;
                            JogMovement.StopJogRobotThread();
                            robot.RobotEnable(0);
                        }

                        if (Convert.ToInt16(e.NewValue) == 1) // Auto
                        {
                            robotCycleStopRequested = false;
                            JogMovement.StopJogRobotThread();
                            robot.RobotEnable(1);
                            SetRobotMode(0);
                        }

                        if (Convert.ToInt16(e.NewValue) == 2) // Man
                        {
                            if (robotInPause)
                            {
                                robot.ResumeMotion();

                            }

                            SetRobotMode(1);
                            JogMovement.StartJogRobotThread();
                        }

                        break;
                        */
                }
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

                // Resetto contatore posizionamento catena
                RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");

            }

            TriggerAllarmeResettato();

            RefresherTask.AddUpdate(PLCTagName.System_error, 0, "INT16");

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
        private static int placePosition = 1;
        /// <summary>
        /// Metodo per spostare il robot lungo gli assi X e Y di una quantità specificata utilizzando la cinematica diretta e inversa.
        /// </summary>
        /// <param name="approachPoint">Posizione dei giunti di avvicinamento.</param>
        /// <param name="xIncrement">Quantità di incremento lungo l'asse X in millimetri.</param>
        /// <param name="yIncrement">Quantità di incremento lungo l'asse Y in millimetri.</param>
        public static async Task MoveRobotAlongXYAxis(DescPose pPlaceOrigine, DescPose approachPoint, double xIncrement, double yIncrement)
        {
            var restPose = ApplicationConfig.applicationsManager.GetPosition("pHome");
            var attesaPick = ApplicationConfig.applicationsManager.GetPosition("pPick");

            DescPose rest = new DescPose(restPose.x, restPose.y, restPose.z, restPose.rx, restPose.ry, restPose.rz);

            DescPose prePick = new DescPose(attesaPick.x, attesaPick.y, attesaPick.z + 50, attesaPick.rx, attesaPick.ry, attesaPick.rz);

            DescPose place = new DescPose(0, 0, 0, 0, 0, 0);
            // Offset dell'asse Z
            double heightOffset = 0.0;
            // Creazione della posa con inserimento heightOffset
            place = new DescPose(pPlaceOrigine.tran.x + xIncrement, pPlaceOrigine.tran.y + yIncrement, pPlaceOrigine.tran.z + heightOffset,
                pPlaceOrigine.rpy.rx, pPlaceOrigine.rpy.ry, pPlaceOrigine.rpy.rz);

            inPosition = false;

            endingPoint = place;

            await GoToPlaceApproachPose(approachPoint, xIncrement, yIncrement);

            // await Task.Delay(200);

            //  WaitForRobotToFinish();

            // robot.SetSpeed(20);

            await GoToPlacePose(pPlaceOrigine, xIncrement, yIncrement);



            //  WaitForRobotToFinish();



            while (!inPosition)
            {
                // Attendo comando apertura pinze
            }



            // Apro le pinze
            RefresherTask.AddUpdate("PLC1_" + "bOpenGripper", true, "BOOL");
            RefresherTask.AddUpdate("PLC1_" + "bCloseGripper", false, "BOOL");

            while (!gripperOpened)
            {
                // Attendo feedback di pinze aperte
            }

            // chiudo le pinze
            RefresherTask.AddUpdate("PLC1_" + "bOpenGripper", false, "BOOL");
            RefresherTask.AddUpdate("PLC1_" + "bCloseGripper", true, "BOOL");

            await Task.Delay(300);


            // Apro le pinze
            RefresherTask.AddUpdate("PLC1_" + "bOpenGripper", true, "BOOL");
            RefresherTask.AddUpdate("PLC1_" + "bCloseGripper", false, "BOOL");

            //  await Task.Delay(1500);

            /* // Apro le pinze
             RefresherTask.AddUpdate("PLC1_" + "bOpenGripper", true, "BOOL");
             RefresherTask.AddUpdate("PLC1_" + "bCloseGripper", false, "BOOL");

             while (!gripperOpened)
             {
                 // Attendo feedback di pinze aperte
             }

             // chiudo le pinze
             RefresherTask.AddUpdate("PLC1_" + "bOpenGripper", false, "BOOL");
             RefresherTask.AddUpdate("PLC1_" + "bCloseGripper", true, "BOOL");

             while (!gripperClosed)
             {
                 // Attendo feedback di pinze aperte
             }



             // Apro le pinze
             RefresherTask.AddUpdate("PLC1_" + "bOpenGripper", true, "BOOL");
             RefresherTask.AddUpdate("PLC1_" + "bCloseGripper", false, "BOOL");

             while (!gripperOpened)
             {
                 // Attendo feedback di pinze aperte
             }*/

            // Reset delle variabili
            gripperOpened = false;
            openGripper = false;
            RefresherTask.AddUpdate("PLC1_" + "bOpenGripper", false, "BOOL");

            placePosition++;



            //  WaitForRobotToFinish();

            //  robot.SetSpeed(speedRobot);

            await GoToPlaceApproachPose(approachPoint, xIncrement, yIncrement);

            // Esegui la routine di riposo
            await RestRoutine(rest);

            await GoToPrePick(prePick);
        }

        /// <summary>
        /// Routine di Place
        /// </summary>
        public static async Task<bool> PlaceRoutine(bool presenzaTeglia1, bool presenzaTeglia2)
        {
            #region Dichiarazione punti di avvicinamento 
            // Array di punti di origine
            DescPose[] placePoints = new DescPose[]
            {
                new DescPose(0,0,0,0,0,0),
                new DescPose(0,0,0,0,0,0)
            };

            // Array di punti di avvicinamento
            DescPose[] approachPoints = new DescPose[]
            {
                new DescPose(0,0,0,0,0,0),
                new DescPose(0,0,0,0,0,0)
            };

            // Ottieni la posizione del punto di avvicinamento a pPlaceOrigine_1
            var pose = ApplicationConfig.applicationsManager.GetPosition("pPlaceOrigine_1");
            placePoints[0] = new DescPose(pose.x, pose.y, pose.z, pose.rx, pose.ry, pose.rz);
            approachPoints[0] = new DescPose(pose.x, pose.y, pose.z + 50, pose.rx, pose.ry, pose.rz);

            // Ottieni la posizione del punto di avvicinamento a pPlaceOrigine_1
            pose = ApplicationConfig.applicationsManager.GetPosition("pPlaceOrigine_2");
            placePoints[1] = new DescPose(pose.x, pose.y, pose.z, pose.rx, pose.ry, pose.rz);
            approachPoints[1] = new DescPose(pose.x, pose.y, pose.z + 50, pose.rx, pose.ry, pose.rz);
            #endregion

            bool[] presenzaTeglia = new bool[] { Convert.ToBoolean(presenzaTeglia1), Convert.ToBoolean(presenzaTeglia2) };

            int tegliaInLavorazione = -1;

            // Verifica quale teglia è in lavorazione e non è piena
            for (int i = 0; i < teglieInLavorazione.Length; i++)
            {
                if (teglieInLavorazione[i]
                    && !tegliaFull[i]
                    && presenzaTeglia[i])
                {
                    tegliaInLavorazione = i;

                    RefresherTask.AddUpdate("PLC1_" + "bPlate" + (i + 1) + "_Filling", true, "BOOL");

                    break; // Esce dal ciclo non appena trova una teglia in lavorazione e non piena
                }
            }

            // Se nessuna teglia è in lavorazione, imposta tegliaInLavorazione su 0
            if (tegliaInLavorazione == -1)
            {
                for (int i = 0; i < teglieInLavorazione.Length; i++)
                {
                    if (!tegliaFull[i]
                        && presenzaTeglia[i])
                    {
                        tegliaInLavorazione = i;

                        RefresherTask.AddUpdate("PLC1_" + "bPlate" + (i + 1) + "_Filling", true, "BOOL");

                        teglieInLavorazione[tegliaInLavorazione] = true; // Imposta la teglia come in lavorazione
                        break;
                    }
                }
            }

            if (tegliaInLavorazione == -1)
            {
                log.Info("Nessuna teglia disponibile per il posizionamento.");
                return false;
            }



            // Calcola l'indice corrente basato su riga e colonna
            int indiceCorrente = yCurrent[tegliaInLavorazione] * xLimit + xCurrent[tegliaInLavorazione];
            double x = xCurrent[tegliaInLavorazione] * xOffset;
            double y = yCurrent[tegliaInLavorazione] * yOffset;

            await MoveRobotAlongXYAxis(
                placePoints[tegliaInLavorazione],
                approachPoints[tegliaInLavorazione],
                x,
                y);

            // Aggiorna la posizione corrente
            xCurrent[tegliaInLavorazione]++;


            if (xCurrent[tegliaInLavorazione] >= xLimit)
            {
                yCurrent[tegliaInLavorazione]++;

                xCurrent[tegliaInLavorazione] = 0;

                if (yCurrent[tegliaInLavorazione] >= yLimit)
                {
                    yCurrent[tegliaInLavorazione] = 0; // Reset a zero se abbiamo completato l'intera matrice
                    tegliaFull[tegliaInLavorazione] = true; // Imposta la teglia come piena

                    RefresherTask.AddUpdate("PLC1_" + "bPlate" +
                     (tegliaInLavorazione + 1) + "_Full", true, "BOOL");

                    teglieInLavorazione[tegliaInLavorazione] = false; // Imposta la teglia come non in lavorazione
                }
            }

            contTrays[tegliaInLavorazione] += 1;
            RefresherTask.AddUpdate("PLC1_" + "iNrPosTeglia" + (tegliaInLavorazione + 1).ToString(),
                  contTrays[tegliaInLavorazione], "INT16");

            return true;
        }

        private static int[] contTrays = new int[] { 0, 0 };

        /// <summary>
        /// Movimento del robot al punto PickApproach
        /// </summary>
        /// <returns></returns>
        public static async Task GoToPickApproachPose(DescPose pickApproachPose)
        {
            RobotManager.robot.MoveCart(pickApproachPose, tool, user, vel, acc, ovl, blendT, config);
        }

        /// <summary>
        /// Movimento del robot al punto PickPose
        /// </summary>
        /// <returns></returns>
        public static async Task GoToPickPose(DescPose pick)
        {
            robot.MoveCart(pick, tool, user, vel, acc, ovl, blendT, config);
        }

        /// <summary>
        /// Movimento del Robot al punto PlaceApproach
        /// </summary>
        /// <param name="approachPoint">Punto di avvicinamento</param>
        /// <param name="xIncrement">x offset</param>
        /// <param name="yIncrement">y offset</param>
        /// <returns></returns>
        public static async Task GoToPlaceApproachPose(DescPose approachPoint, double xIncrement, double yIncrement)
        {
            // Creazione della nuova posizione con gli assi X e Y incrementati
            DescPose newPosition = new DescPose(approachPoint.tran.x, approachPoint.tran.y, approachPoint.tran.z,
                approachPoint.rpy.rx, approachPoint.rpy.ry, approachPoint.rpy.rz)
            {
                tran = { x = approachPoint.tran.x + xIncrement, y = approachPoint.tran.y + yIncrement } // Incrementa gli assi X e Y di xIncrement e yIncrement mm
            };

            // Muovi il robot ai nuovi valori dei giunti
            err = robot.MoveCart(newPosition, tool, user, vel, acc, ovl, blendT, config);

        }

        public static async Task GoToPrePick(DescPose prePick)
        {
            // Muovi il robot ai nuovi valori dei giunti
            err = robot.MoveCart(prePick, tool, user, vel, acc, ovl, blendT, config);

        }

        /// <summary>
        /// Movimento del robot al punto PlacePose
        /// </summary>
        /// <param name="xIncrement">x offset</param>
        /// <param name="yIncrement">y offset</param>
        /// <returns></returns>
        public static async Task GoToPlacePose(DescPose pPlaceOrigine, double xIncrement, double yIncrement)
        {
            DescPose place = new DescPose(0, 0, 0, 0, 0, 0);

            // Offset dell'asse Z
            double heightOffset = 0.0;

            // Creazione della posa con inserimento heightOffset
            place = new DescPose(pPlaceOrigine.tran.x + xIncrement, pPlaceOrigine.tran.y + yIncrement, pPlaceOrigine.tran.z + heightOffset,
                pPlaceOrigine.rpy.rx, pPlaceOrigine.rpy.ry, pPlaceOrigine.rpy.rz);

            // Muove il robot alla posizione di Place per appoggiare i cannoli
            robot.MoveCart(place, tool, user, vel, acc, ovl, blendT, config);

        }

        /// <summary>
        /// Routine di riposo
        /// </summary>
        public static async Task RestRoutine(DescPose rest)
        {
            robot.MoveCart(rest, tool, user, vel, acc, ovl, blendT, config);
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
                        RefresherTask.AddUpdate(PLCTagName.Emergency, 1, "INT16");

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
                    RefresherTask.AddUpdate(PLCTagName.Emergency, 0, "INT16");
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
        /// Aggiorna il contatore che indica lo spostamento della catena
        /// </summary>
        public static void CheckChainPos()
        {
            if (!stopChainUpdaterThread)
            {
                // Get del valore dello spostamento catena dal dizionario di variabili PLC
                chainPos = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.Chain_Pos));
            }
            else
            {
                chainPos = 0;
            }
        }

       static  List<DescPose> InterpolateLinear(DescPose p1, DescPose p2, int steps)
        {
            List<DescPose> points = new List<DescPose>();
            for (int i = 1; i < steps; i++) // salta p1 (già incluso), evita p2 (lo invii a parte)
            {
                double t = (double)i / steps;
                DescPose pt = new DescPose
                (
                    p1.tran.x + (p2.tran.x - p1.tran.x) * t,
                    p1.tran.y + (p2.tran.y - p1.tran.y) * t,
                    p1.tran.z + (p2.tran.z - p1.tran.z) * t,
                    p1.rpy.rx + (p2.rpy.rx - p1.rpy.rx) * t,
                    p1.rpy.ry + (p2.rpy.ry - p1.rpy.ry) * t,
                    p1.rpy.rz + (p2.rpy.rz - p1.rpy.rz) * t
                );

                points.Add(pt);
            }
            return points;
        }

        static void CalcolaPosizioneFocacce(double larghezzaFocaccia, double profonditaFocaccia,
                                     double larghezzaPallet, double profonditaPallet)
        {
            // Calcola quante focacce possono entrare in larghezza e profondità
            int numeroFocacceLungoPalletX = (int)(larghezzaPallet / larghezzaFocaccia);
            int numeroFocacceLungoPalletY = (int)(profonditaPallet / profonditaFocaccia);

            // Calcola il numero totale di focacce che possono essere posizionate sulla superficie del pallet
            int numeroTotaleFocacce = numeroFocacceLungoPalletX * numeroFocacceLungoPalletY;

            // Stampa i risultati
            Console.WriteLine($"Numero di focacce in larghezza: {numeroFocacceLungoPalletX}");
            Console.WriteLine($"Numero di focacce in profondità: {numeroFocacceLungoPalletY}");
            Console.WriteLine($"Numero totale di focacce che puoi mettere sul pallet: {numeroTotaleFocacce}");
        }


        /// <summary>
        /// Esegue ciclo saldatura
        /// </summary>
        public static async void PickAndPlaceFocaccia()
        {
            #region Dichiarazione punti routine

            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset

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
            bool appoggiaSuScatola = true;

            // Segnalo al PLC che il robot sta lavorando in modalità automatica
            RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 1, "INT16");
            RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 0, "INT16");

            int riga = 0;
            int colonna = 0;
            int strato = 0;
            int larghezzaFocaccia = 300;
            int profonditaFocaccia = 300;
            int altezzaStrato = 100;
            int larghezzaPallet = 800;
            int profonditaPallet = 600;
            int numeroRighe = (int)(larghezzaPallet / larghezzaFocaccia);
            int numeroColonne = (int)(profonditaPallet / profonditaFocaccia);

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

                                // Segnalo al PLC che il robot ha terminato il lavoro in modalità automatica
                                RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");
                                RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 1, "INT16");

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

                                /*
                                // STEP 2: Inserimento punti intermedi tra Home e ApproachPick
                                int numSteps = 10; // Numero di punti interpolati (puoi regolare)
                                List<DescPose> intermediatePoints = InterpolateLinear(descPosHome, descPosApproachPick, numSteps);

                                foreach (var point in intermediatePoints)
                                {
                                    movementResult = robot.MoveCart(point, tool, user, vel, acc,
                                        ovl, blendT, config);
                                    GetRobotMovementCode(movementResult);
                                }
                                */

                                // STEP 3: Invio punto di avvicinamento Pick
                                movementResult = robot.MoveCart(descPosApproachPick, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);

                                // STEP 4: Movimento lineare finale
                                movementResult = robot.MoveL(jointPosPick, descPosPick, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset);
                                GetRobotMovementCode(movementResult);

                                // Optional: movimento cartesiano alternativo
                                /*
                                movementResult = robot.MoveCart(descPosPick, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);
                                */

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
                                RefresherTask.AddUpdate(PLCTagName.GripperStatusOut, 0, "INT16");
                                step = 40;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 30 - Attesa inPosition punto di Pick");
                            break;

                        #endregion

                        case 40:
                            #region Presa focaccia

                            gripperStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.GripperStatusIn));

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

                            if (appoggiaSuScatola)
                            {
                                inPosition = false; // Reset inPosition

                                DescPose puntoFocaccia = CalcolaPosizioneFocaccia(
                                    riga,
                                    colonna,
                                    strato,
                                    larghezzaFocaccia,
                                    profonditaFocaccia,
                                    altezzaStrato,
                                    originePallet
                                );

                                RobotManager.robot.GetInverseKin(0, puntoFocaccia, -1, ref jointPosPlaceCalculated);
                                
                                descPosApproachPlace = new DescPose(
                                    puntoFocaccia.tran.x,
                                    puntoFocaccia.tran.y,
                                    puntoFocaccia.tran.z + 200,
                                    puntoFocaccia.rpy.rx,
                                    puntoFocaccia.rpy.ry,
                                    puntoFocaccia.rpy.rz);

                                RobotManager.robot.GetInverseKin(0, descPosApproachPlace, -1, ref jointPosApproachPlace);

                                movementResult = robot.MoveCart(descPosApproachPlace, tool, user, vel, acc,
                                  ovl, blendT, config); // Invio punto di avvicinamento place
                                GetRobotMovementCode(movementResult);

                                // Movimento lineare
                                movementResult = robot.MoveL(jointPosPlaceCalculated, puntoFocaccia, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset
                                    );


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

                                endingPoint = puntoFocaccia; // Assegnazione endingPoint

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
                                RefresherTask.AddUpdate(PLCTagName.GripperStatusOut, 0, "INT16");
                                step = 90;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 80 - Attesa inPosition punto di place");
                            break;
                        #endregion

                        case 90:
                            #region Rilascio focaccia

                            gripperStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.GripperStatusIn));

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
        /// Esegue ciclo saldatura
        /// </summary>
        public static async void PickAndPlaceTeglia()
        {
            #region Dichiarazione punti routine

            DescPose offset = new DescPose(0, 0, 0, 0, 0, 0); // Nessun offset

            #region Punto home

            var home = ApplicationConfig.applicationsManager.GetPosition("pHomeTeglia", "RM");
            DescPose descPosHome = new DescPose(home.x, home.y, home.z, home.rx, home.ry, home.rz);

            #endregion

            #region Punto di pick

            JointPos jointPosPick = new JointPos(0, 0, 0, 0, 0, 0);
            var pick = ApplicationConfig.applicationsManager.GetPosition("pPickTeglia", "RM");
            DescPose descPosPick = new DescPose(pick.x, pick.y, pick.z, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPick, -1, ref jointPosPick);

            #endregion

            #region Punto avvicinamento pick

            JointPos jointPosApproachPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosApproachPick = new DescPose(pick.x + 550, pick.y, pick.z - 40, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPick, -1, ref jointPosApproachPick);

            #endregion

            #region Punto allontanamento post pick

            JointPos jointPosAllontanamentoPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPick = new DescPose(descPosApproachPick.tran.x, descPosApproachPick.tran.y,
                descPosApproachPick.tran.z + 80, descPosApproachPick.rpy.rx, descPosApproachPick.rpy.ry, descPosApproachPick.rpy.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPick, -1, ref jointPosAllontanamentoPick);

            #endregion

            #region Punto post pick

            JointPos jointPosPostPick = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPick = new DescPose(pick.x, pick.y, pick.z + 40, pick.rx, pick.ry, pick.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPick, -1, ref jointPosPostPick);

            #endregion

            #region Punto di place

            JointPos jointPosPlace = new JointPos(0, 0, 0, 0, 0, 0);
            var place = ApplicationConfig.applicationsManager.GetPosition("pPlaceTeglia", "RM");
            DescPose descPosPlace = new DescPose(place.x, place.y, place.z, place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPlace, -1, ref jointPosPlace);

            #endregion

            #region Punto avvicinamento place

            JointPos jointPosApproachPlace = new JointPos(0, 0, 0, 0, 0, 0);
            var approachplace = ApplicationConfig.applicationsManager.GetPosition("pAvvicinamentoPlaceTeglia", "RM");
            DescPose descPosApproachPlace = new DescPose(place.x + 550, place.y, place.z + 40,
                place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosApproachPlace, -1, ref jointPosApproachPlace);

            #endregion

            #region Punto allontanamento place

            JointPos jointPosAllontanamentoPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosAllontanamentoPlace = new DescPose(place.x + 550, place.y, place.z - 40,
                place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosAllontanamentoPlace, -1, ref jointPosAllontanamentoPlace);

            #endregion

            #region Punto post place

            JointPos jointPosPostPlace = new JointPos(0, 0, 0, 0, 0, 0);
            DescPose descPosPostPlace = new DescPose(place.x, place.y, place.z - 10,
                place.rx, place.ry, place.rz);
            RobotManager.robot.GetInverseKin(0, descPosPostPlace, -1, ref jointPosPostPlace);

            #endregion

          

            #endregion

            #region Parametri movimento

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
            bool appoggiaSuScatola = true;

            // Segnalo al PLC che il robot sta lavorando in modalità automatica
            RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 1, "INT16");
            RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 0, "INT16");

            int riga = 0;
            int colonna = 0;
            int strato = 0;
            int larghezzaFocaccia = 300;
            int profonditaFocaccia = 300;
            int altezzaStrato = 100;
            int larghezzaPallet = 800;
            int profonditaPallet = 600;
            int numeroRighe = (int)(larghezzaPallet / larghezzaFocaccia);
            int numeroColonne = (int)(profonditaPallet / profonditaFocaccia);

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

                                // Segnalo al PLC che il robot ha terminato il lavoro in modalità automatica
                                RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");
                                RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 1, "INT16");

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

                                /*
                                // STEP 2: Inserimento punti intermedi tra Home e ApproachPick
                                int numSteps = 10; // Numero di punti interpolati (puoi regolare)
                                List<DescPose> intermediatePoints = InterpolateLinear(descPosHome, descPosApproachPick, numSteps);

                                foreach (var point in intermediatePoints)
                                {
                                    movementResult = robot.MoveCart(point, tool, user, vel, acc,
                                        ovl, blendT, config);
                                    GetRobotMovementCode(movementResult);
                                }
                                */

                                // STEP 3: Invio punto di avvicinamento Pick
                                movementResult = robot.MoveCart(descPosApproachPick, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);

                                // STEP 4: Movimento lineare finale
                                movementResult = robot.MoveL(jointPosPick, descPosPick, tool, user, vel, acc,
                                    ovl, blendT, epos, 0, offsetFlag, offset);
                                GetRobotMovementCode(movementResult);

                                // Optional: movimento cartesiano alternativo
                                /*
                                movementResult = robot.MoveCart(descPosPick, tool, user, vel, acc,
                                    ovl, blendT, config);
                                GetRobotMovementCode(movementResult);
                                */

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
                                RefresherTask.AddUpdate(PLCTagName.GripperStatusOut, 1, "INT16");
                                step = 40;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 30 - Attesa inPosition punto di Pick");
                            break;

                        #endregion

                        case 40:
                            #region Presa focaccia

                            gripperStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.GripperStatusIn));

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

                            movementResult = robot.MoveL(jointPosPostPick, descPosPostPick, tool, user, vel, acc,
                                   ovl, blendT, epos, 0, offsetFlag, offset);
                            GetRobotMovementCode(movementResult);

                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosAllontanamentoPick, descPosAllontanamentoPick, tool, user, vel, acc,
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
                                RefresherTask.AddUpdate(PLCTagName.GripperStatusOut, 0, "INT16");
                                step = 90;
                            }

                            formDiagnostics.UpdateRobotStepDescription("STEP 80 - Attesa inPosition punto di place");
                            break;
                        #endregion

                        case 90:
                            #region Rilascio focaccia

                            gripperStatus = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.GripperStatusIn));

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
                            movementResult = robot.MoveL(jointPosPostPlace, descPosPostPlace, tool, user, vel, acc,
                                ovl, blendT, epos, 0, offsetFlag, offset
                                );


                            // Movimento lineare
                            movementResult = robot.MoveL(jointPosAllontanamentoPlace, descPosAllontanamentoPlace, tool, user, vel, acc,
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

        static DescPose CalcolaPosizioneFocaccia(
    int riga,
    int colonna,
    int strato,
    double larghezzaFocaccia,
    double profonditaFocaccia,
    double altezzaStrato,
    DescPose originePallet)
        {
            double x = originePallet.tran.x + (colonna * larghezzaFocaccia) + (larghezzaFocaccia / 2.0);
            double y = originePallet.tran.y + (riga * profonditaFocaccia) + (profonditaFocaccia / 2.0);
            double z = originePallet.tran.z + (strato * altezzaStrato);

            double rx = originePallet.rpy.rx;
            double ry = originePallet.rpy.ry;
            double rz = originePallet.rpy.rz;

            return new DescPose(x, y, z, rx, ry, rz);
        }


        /// <summary>
        /// A true quando il ciclo deve riprendere da un punto precedente
        /// </summary>
        public static bool riprendiCiclo = false;

        /// <summary>
        /// Avvia la riproduzione dei punti dell'applicazione selezionata
        /// </summary>
        /// <param name="application">Applicazione selezionata</param>
        public static async void StartApplication(string application)
        {
            // Reset della condizione per terminare il thread che gestisce colorazione su lw_positions
            stopPositionCheckerThread = false;

            // positionsToCheck.Clear(); // Reset della lista che viene letta da positionCheckerThread
            UC_FullDragModePage.stopMonitoring = true; // Stop del thread monitoring nel caso sia attivo

            // Thread che esegue selezione su lw_positions e scrittura valori pistole (position checker)
            positionCheckerThread = new Thread(new ThreadStart(CheckPosition));
            positionCheckerThread.IsBackground = true;
            positionCheckerThread.Priority = ThreadPriority.Normal;
            positionCheckerThread.Start(); // Avvio thread

            stopCycleRequested = false; // Reset della richiesta di stop della riproduzione dei punti

            pauseCycleRequested = false; // Reset della richiesta di pausa della riproduzione dei punti

            stopCycleRoutine = false; // A true quando la routine termina

            stopChainUpdaterThread = false; // A true quando bisogna disattivare l'updater del contatore della catena

            int thresholdPos = 5; // Soglia di posizioni da eseguire

            // Indice della posizione presente nella lista delle posizione da riprodurre su cui eseguire inPosition 
            int calculateIndex = 2;

            int step = 0; // Step routine

           

            // Get della posizione di home dal dizionario delle posizioni
            var homePose = ApplicationConfig.applicationsManager.GetPosition("pHome", "RM");

            int index = 0; // Indice della posizione

            int indexEndingPoint = 0; // Indice della posizione su cui eseguire inPosition

            DescPose targetPos = new DescPose(0, 0, 0, 0, 0, 0); // Posizione da riprodurre

            // Creazione del punto di Home
            DescPose pHome = new DescPose(
                homePose.x,
                homePose.y,
                homePose.z,
                homePose.rx,
                homePose.ry,
                homePose.rz
                );

            // Get da database delle posizioni dell'applicazione selezionata
            DataTable positions = RobotDAO.GetPointsPosition(ConnectionString, application);

            // Dichiarazione lista che conterrà le posizioni dell'applicazione selezionata
            List<DescPose> pointList = new List<DescPose>();

            // Riempimento lista delle posizioni dell'applicazione selezionata
            foreach (DataRow rw in positions.Rows)
            {
                DescPose pos = new DescPose(
                    Convert.ToDouble(rw["x"]),
                    Convert.ToDouble(rw["y"]),
                    Convert.ToDouble(rw["z"]),
                    Convert.ToDouble(rw["rx"]),
                    Convert.ToDouble(rw["ry"]),
                    Convert.ToDouble(rw["rz"])
                );

                pointList.Add(pos);
            }

            // Inizializzazione della variabile currentIndex in modo che parta dall'indice corrente
            if (currentIndex < 0)
                index = 0;
            else if (currentIndex >= positions.Rows.Count - 1)
            {
                index = 0;
            }
            else
                index = currentIndex + 1;

            
            // Faccio girare processo su un thread esterno a quello principale
            await Task.Run(async () =>
            {
                // Reset variabile che fa partire il contatore della catena
                RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");
                Thread.Sleep(200); // Leggero ritardo per stabilizzare il sistema

                // Imposto a 1 (true) Automatic_Start, così che parta anche il conteggio dello spostamento della catena
                RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 1, "INT16");

                // Imposto a 0 (false) Auto_Cycle_End che segnala che il ciclo automatico è iniziato
                RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 0, "INT16");

                // Lista delle posizioni da riprodurre 
                List<DescPose> targetPositions = new List<DescPose>();

                for (int i = 0; i < pointList.Count; i++)
                {
                    targetPos = new DescPose(
                                pointList[i].tran.x,
                                pointList[i].tran.y,
                                pointList[i].tran.z,
                                pointList[i].rpy.rx,
                                pointList[i].rpy.ry,
                                pointList[i].rpy.rz);

                    targetPositions.Add(targetPos);
                }

                // Fino a quando non viene eseguita una richiesta di stop routine e non sono presenti allarmi bloccanti
                while (!stopCycleRoutine && !AlarmManager.blockingAlarm)
                {
                    switch (step)
                    {
                        case 0:
                            #region Invio delle posizioni da riprodurre

                            if (index <= pointList.Count - 1 && !stopCycleRequested)
                            {
                                // Calcolo dell'indice della posizione su cui eseguire inPosition
                                indexEndingPoint = index + calculateIndex;

                                // Reset inPosition
                                inPosition = false;

                                aggiornamentoFinito = false;

                                // Riproduco le posizioni che partono dall'indice in cui si trova la routine
                                // all'indice composto dalla somma tra l'indice e la soglia che indica il numero di posizioni da riprodurre
                                for (int i = index; i < index + thresholdPos; i++)
                                {
                                    if (i <= pointList.Count - 1)
                                    {
                                        targetPos = new DescPose(
                                            targetPositions[i].tran.x,
                                            targetPositions[i].tran.y,
                                            targetPositions[i].tran.z,
                                            targetPositions[i].rpy.rx,
                                            targetPositions[i].rpy.ry,
                                            targetPositions[i].rpy.rz
                                            );

                                        positionsToCheck.Add(new KeyValuePair<int, DescPose>(i, targetPos));
                                    }
                                }
                                aggiornamentoFinito = true;

                                // Riproduco le posizioni che partono dall'indice in cui si trova la routine
                                // all'indice composto dalla somma tra l'indice e la soglia che indica il numero di posizioni da riprodurre
                                for (int i = index; i < index + thresholdPos; i++)
                                {
                                    if (i <= pointList.Count - 1)
                                    {
                                        targetPos = new DescPose(
                                           targetPositions[i].tran.x,
                                           targetPositions[i].tran.y,
                                           targetPositions[i].tran.z,
                                           targetPositions[i].rpy.rx,
                                           targetPositions[i].rpy.ry,
                                           targetPositions[i].rpy.rz
                                           );

                                        // Se l'indice della posizione creata corrisponde all'indice
                                        // della posizione su cui eseguire l'inPosition, assegno questo punto come endingPoint
                                        if (i == indexEndingPoint)
                                            endingPoint = targetPos;

                                        // Invio delle posizione al robot
                                        err = robot.MoveCart(targetPos, 0, user, vel, acc, ovl, blendT, config);

                                        if (err != 0) // Se il movimento ha generato un errore
                                            log.Error("Errore durante il movimento del robot: " + err.ToString());
                                    }
                                }

                                step = 10;
                            }
                            else // Se non ci sono più posizioni da riprodurre
                            {
                                // Se è stata richiesto lo stop immediato del ciclo
                                if (stopCycleRequested)
                                {
                                    step = 5;
                                }
                                else
                                    if (index > pointList.Count - 1) // Se i punti sono terminati, riavvio il ciclo
                                {
                                    index = 0;
                                    step = 0;
                                    chainPos = 0; // Azzero spostamento catena
                                    stopChainUpdaterThread = true; // Fermo aggiornamento catena
                                }
                            }

                            break;

                        #endregion

                        case 5:
                            #region Termine routine

                            positionsToCheck.Clear();

                            stopPositionCheckerThread = true;

                            // Imposto a 0 (false) Automatic_Start che resetta anche il contatore dello spostamento della catena
                            RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");

                            // Imposto a 1 (true) Auto_Cycle_End che segnala che il ciclo automatico è terminato
                            RefresherTask.AddUpdate(PLCTagName.Auto_Cycle_End, 1, "INT16");

                            // Abilito il tasto Start per avviare nuovamente la routine
                            EnableButtonCycleEvent?.Invoke(1, EventArgs.Empty);

                            // Imposto a false il booleano che fa terminare il thread della routine
                            stopCycleRoutine = true;

                            // Fermo thread per aggiornamento posizione catena
                            stopChainUpdaterThread = true;

                            break;

                        #endregion

                        case 10:
                            #region Attesa inPosition, aggiornamento index e check richiesta di stop/pausa

                            if (inPosition) // Se il robot è arrivato all'ending point
                            {
                                // Aggiornamento index
                                index = index + thresholdPos;

                                if (positionsToCheck.Count > numPositionsToCheck)
                                {
                                    positionsToCheck.RemoveRange(0, 2);
                                }

                                step = 0;
                            }


                            // Se è stata richiesta lo stop del ciclo, rimando subito allo step 0,
                            // così che venga termino il thread
                            if (stopCycleRequested)
                            {
                                step = 0;
                            }

                            // Se è stata richiesta la pausa del ciclo, aspetto di arrivare alla posizione
                            // subito successiva alla quale si trova il Robot
                            if (pauseCycleRequested)
                            {
                                inPosition = false;
                                if (currentIndex < targetPositions.Count - 1)
                                {
                                    // Se la pausa viene richiesta non all'ultimo punto,
                                    // metto come endingPoint il punto successivo
                                    endingPoint = targetPositions[currentIndex + 1];
                                }
                                else
                                {
                                    // Se la pausa viene richiesta all'ultimo punto,
                                    // metto come endingPoint il primo punto della lista
                                    endingPoint = targetPositions[0];
                                }

                                step = 15;
                            }
                            break;

                        #endregion

                        case 15:
                            #region Pausa del Robot

                            if (inPosition)
                            {
                                // Stop del Robot
                                robot.PauseMotion();
                                Thread.Sleep(200);
                                robot.StopMotion();

                                inPosition = false; // Reset inPosition

                                // Abilito il tasto Start per avviare nuovamente la routine
                                EnableButtonCycleEvent?.Invoke(1, EventArgs.Empty);

                                step = 20;
                            }
                            break;

                        #endregion

                        case 20:
                            #region Ripresa del ciclo

                            if (riprendiCiclo)
                            {
                                riprendiCiclo = false;
                                pauseCycleRequested = false; // Reset richiesta di pausa
                                index = currentIndex + 1;
                                EnableButtonCycleEvent?.Invoke(0, EventArgs.Empty);
                                step = 0;
                            }
                            break;

                            #endregion
                    }

                    Thread.Sleep(10); // Delay routine
                }
            });
        }

        /// <summary>
        /// Avvia la riproduzione dei punti dell'applicazione selezionata
        /// </summary>
        /// <param name="application">Applicazione selezionata</param>
        public static async void _StartApplication(string application)
        {


            int offsetChain = 0;

            int tresholdPos = 4;
            //int calculateIndex = (int)Math.Ceiling((double)tresholdPos / 2);
            int calculateIndex = 2;

            int step = 0;
            var homePose = ApplicationConfig.applicationsManager.GetPosition("pHome", "RM");
            int index = 0;

            DescPose targetPos = new DescPose(0, 0, 0, 0, 0, 0);

            DescPose pHome = new DescPose(
                homePose.x,
                homePose.y,
                homePose.z,
                homePose.rx,
                homePose.ry,
                homePose.rz
                );

            DataTable positions = RobotDAO.GetPointsPosition(ConnectionString, application);
            List<DescPose> pointList = new List<DescPose>();
            bool stopRoutine = false;

            foreach (DataRow rw in positions.Rows)
            {
                DescPose pos = new DescPose(
                    Convert.ToDouble(rw["x"]),
                    Convert.ToDouble(rw["y"]),
                    Convert.ToDouble(rw["z"]),
                    Convert.ToDouble(rw["rx"]),
                    Convert.ToDouble(rw["ry"]),
                    Convert.ToDouble(rw["rz"])
                );

                pointList.Add(pos);
            }

            // Faccio girare processo su un thread esterno a quello principale
            await Task.Run(async () =>
            {
                RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 1, "INT16");
                Queue<DescPose> commandQueue = new Queue<DescPose>();

                while (!stopRoutine)
                {
                    switch (step)
                    {
                        case 0:

                            #region Check termine ciclo

                            // Se sono terminate le posizioni da riprodurre, termino il task, rimando il robot in HomePosition
                            // e imposto la variabile Automatic_Start a 0 (false) così da resettare anche 
                            // il contatore della posizione della catena (Chain_Pos)
                            if (index > pointList.Count - 1)
                            {
                                // Rimando il robot in HomePosition
                                GoToHomePosition();

                                inPosition = false;
                                endingPoint = pHome;

                                step = 1;
                            }
                            else
                            {
                                // Se le posizioni non sono terminate, procedo con lo step successivo
                                step = 5;
                            }
                            break;

                        #endregion

                        case 1:

                            if (inPosition)
                            {
                                // Imposto a true booleano che serve per terminare il task
                                stopRoutine = true;

                                // Reset indice posizione 
                                index = 0;

                                // Reset variabile Automatic_Start
                                RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");

                                // Reset step routine
                                step = 0;

                                // Invoco evento necessario a riattivare in Home Page il button per far ripartire il ciclo
                                EnableButtonCycleEvent?.Invoke(null, EventArgs.Empty);
                            }

                            break;

                        case 5:



                            // Aggiorniamo offsetChain dinamicamente
                            offsetChain = Convert.ToInt16(PLCConfig.appVariables.getValue(PLCTagName.Chain_Pos));

                            targetPos = new DescPose(
                                        pointList[index].tran.x + offsetChain,
                                        pointList[index].tran.y,
                                        pointList[index].tran.z,
                                        pointList[index].rpy.rx,
                                        pointList[index].rpy.ry,
                                        pointList[index].rpy.rz);

                            // Aggiungiamo la posizione target alla coda
                            commandQueue.Enqueue(targetPos);

                            index++;
                            step = 10;
                            break;



                        case 10:
                            // Verifica se la coda non è vuota
                            if (commandQueue.Count > 0)
                            {
                                log.Info(commandQueue.Count.ToString());
                                // Estrarre la prossima posizione dalla coda
                                targetPos = commandQueue.Dequeue();

                                int result = robot.MoveCart(targetPos, 0, user, vel, acc, ovl, blendT, config);

                                inPosition = false;
                                endingPoint = targetPos;

                                if (result != 0)
                                {
                                    log.Error(result.ToString());
                                }

                                WaitForRobotToFinish();
                                step = 0;
                            }
                            else
                            {
                                step = 0;
                            }
                            break;


                        case 20:
                            if (inPosition)
                            {
                                step = 0;
                            }
                            break;
                    }
                    await Task.Delay(20);

                }



                /*
                foreach (DataRow rw in positions.Rows)
                {
                    // Creazione della posizione
                    pos = new DescPose(
                        Convert.ToDouble(rw["x"]),
                        Convert.ToDouble(rw["y"]),
                        Convert.ToDouble(rw["z"]),
                        Convert.ToDouble(rw["rx"]),
                        Convert.ToDouble(rw["ry"]),
                        Convert.ToDouble(rw["rz"]));

                    // Movimento del Robot
                   int result = robot.MoveCart(pos, 0, user, vel, acc, ovl, blendT, config);

                    if (result != 0)
                    {
                        DataRow code = RobotDAO.GetRobotMovementCode(ConnectionString, result);

                        CustomMessageBox.Show(
                            MessageBoxTypeEnum.ERROR,
                            "Fault code: " + code["Fault code"].ToString() + "\nFault name: " + code["Fault name"].ToString() + "\nProcessing method: " + code["Processing method"].ToString()
                            );

                        log.Error("Fault code: " + code["Fault code"].ToString() + "\nFault name: " + code["Fault name"].ToString() + "\nProcessing method: " + code["Processing method"].ToString());
                    }
                }
                log.Info("Punti inviati al Robot");*/
            });
        }

        /// <summary>
        /// Esegue tutti i punti dell'applicazione scelta a partire da uno specifico indice
        /// </summary>
        /// <param name="application"></param>
        /// <param name="startingIndex"></param>
        public static async void StartApplication(string application, int startingIndex)
        {
            // Faccio girare processo su un thread esterno a quello principale
            await Task.Run(() =>
            {
                DescPose pos = new DescPose();
                log.Info("Invio richiesta SQLite: GetPointsPosition");
                //DataTable positions = RobotDAO.GetPointsPosition(ConnectionString, application);

                var positions = ApplicationConfig.applicationsManager.getDictionary()[applicationName].positions;

                if (startingIndex > positions.Count)
                {
                    log.Error($"Tentativo di accesso ad una posizione che non sta dentro all'applicazione, " +
                        $"indice: {startingIndex} " +
                        $"applicazione: {application}");
                    return;
                }

                if (startingIndex < 0)
                    startingIndex = 0;
                else if (startingIndex >= positions.Count - 1)
                    startingIndex = 0;
                else
                    startingIndex++;

                ApplicationPositions rw = null;
                for (int i = startingIndex; i < positions.Count; i++)
                {
                    rw = positions[i];

                    // Creazione della posizione
                    pos = new DescPose(
                        Convert.ToDouble(rw.x),
                        Convert.ToDouble(rw.y),
                        Convert.ToDouble(rw.z),
                        Convert.ToDouble(rw.rx),
                        Convert.ToDouble(rw.ry),
                        Convert.ToDouble(rw.rz));

                    // Movimento del Robot
                    int result = robot.MoveCart(pos, 0, user, vel, acc, ovl, blendT, config);

                    GetRobotMovementCode(result);

                }
                log.Info("Punti inviati al Robot");
            });
        }

        /// <summary>
        /// Annulla il token per fermare il task che gestisce le routine del robot
        /// </summary>
        public static void StopApplication()
        {
            if (normalPriorityThreadStarted)
            {
                stopNormalPriorityThread = true; // Imposta il flag per fermare il thread
                normalPriorityThread.Join(); // Attende che il thread si fermi
            }
        }

        /// <summary>
        /// Usato per muovere il Robot al punto precedente/successivo della lista di punti
        /// </summary>
        /// <param name="pos">Punto da raggiungere</param>
        public static void MoveToPoint(DescPose pos)
        {
            int result = robot.MoveCart(pos, tool, user, vel, acc, ovl, blendT, config);

            GetRobotMovementCode(result);
        }

        /// <summary>
        /// Usato per muovere il Robot al punto selezionato, eseguendo in ordine
        /// anche quelli precedenti all'obiettivo
        /// </summary>
        /// <param name="points">Punto da raggiungere</param>
        public static async void MoveToSelectedPoint(List<DescPose> points)
        {
            int err = 0;

            await Task.Run(() =>
            {
                foreach (DescPose point in points)
                {
                    // Movimento del Robot
                    err = robot.MoveCart(point, tool, user, vel, acc, ovl, blendT, config);
                }
            });
        }

        public static DescPose previousTCPposition = new DescPose(0, 0, 0, 0, 0, 0);

        private static bool isEnable = false;
        private static bool prevIsEnable = false;

        private static bool isNotEnable = false;
        private static bool prevIsNotEnable = false;

        public static bool isEnabledNow = false;

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
                robot.RobotEnable(1);
                prevIsEnable = true;
                prevIsNotEnable = false; // Resetta lo stato "non abilitato"
                AlarmManager.blockingAlarm = false;

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
        public static void CheckRobotPosition(JointPos jPos, JointPos j1_actual_pos, JointPos j2_actual_pos, JointPos j3_actual_pos,
            JointPos j4_actual_pos, JointPos j5_actual_pos, JointPos j6_actual_pos)
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
            var restPose = ApplicationConfig.applicationsManager.GetPosition("pHome", "RM");
            DescPose pHome = new DescPose(restPose.x, restPose.y, restPose.z, restPose.rx, restPose.ry, restPose.rz);
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
        /// Metodo che riattiva Robot
        /// </summary>
        public static void EnableRobot()
        {
            robot.RobotEnable(1);
            robotEnable = true;
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

        /// <summary>
        /// Alza il bool per richiedere la terminazione del thread application al termine del ciclo in corso
        /// </summary>
        public static void RequestStopCycle()
        {
            requestStopCycle = true;
        }

        #region Struttura positionCheckerThread 

        /// <summary>
        /// Thread che esegue il metodo CheckPosition
        /// </summary>
        private static Thread positionCheckerThread;

        /// <summary>
        /// Tempo di refresh all'interno del metodo CheckPosition del thread positionCheckerThread
        /// </summary>
        private static int positionCheckerThreadRefreshPeriod = 50;

        /// <summary>
        /// A true quando il thread deve essere concluso
        /// </summary>
        private static bool stopPositionCheckerThread = false;

        /// <summary>
        /// Checker utilizzato per la colorazione delle righe all'interna di lw_positions
        /// </summary>
        private static PositionChecker checker_monitoringPos = new PositionChecker(50.0);

        /// <summary>
        /// A true quando il punto attuale del robot corrisponde con la posizione interrogata della lw_positions
        /// </summary>
        private static bool inPositionGun = false;

        /// <summary>
        /// Lista di posizioni su cui eseguire l'inPosition per gestire colorazione di lw_positions
        /// </summary>
        private static List<KeyValuePair<int, DescPose>> positionsToCheck = new List<KeyValuePair<int, DescPose>>();

        /// <summary>
        /// Numero di posizioni su cui eseguire l'inPosition e la relativa colorazione su lw_positions
        /// </summary>
        private static int numPositionsToCheck = 5;

        /// <summary>
        /// A true quando l'aggiornamento della lista di posizioni su cui eseguire inPosition 
        /// per gestire colorazione su lw_positions viene terminato
        /// </summary>
        private static bool aggiornamentoFinito = false;

        // Variabile per tracciare l'ultima posizione aggiornata nella ListView
        private static int? lastUpdatedKey = null;


        /// <summary>
        /// Esegue inPosition su una lista di posizioni continuamente aggiornata da CheckMonitoring
        /// </summary>
        private static void CheckPosition()
        {
            stopPositionCheckerThread = false;

            // Contiene indice della riga precedente a quella attuale
            int previousPoint = -1;

            // Lista contenente tutti i gun settings
            List<GunSettings> gunSettings = new List<GunSettings>();

            // Lista di appoggio
            List<ApplicationPositions> positions = ApplicationConfig.applicationsManager.getDictionary()[applicationName].positions;
            List<(string key, int? value, string type)> updates = new List<(string, int?, string)>();



            // Scorri la lista per inserire tutti i gun settings 
            foreach (ApplicationPositions pos in positions)
            {
                gunSettings.Add(pos.gunSettings);
            }


            while (!stopPositionCheckerThread && !AlarmManager.blockingAlarm)
            {
                try
                {
                    // Se siamo all'ultimo punto, resetta il contatore ma NON invocare ancora l'evento
                    if (previousPoint >= positions.Count - 1)
                    {
                        previousPoint = -1; // Resetta solo il contatore
                        RefresherTask.AddUpdate(PLCTagName.Automatic_Start, 0, "INT16");
                        chainPos = 0;
                    }

                    inPositionGun = false; // Reset di inPosition

                    if (aggiornamentoFinito) // Se la lista ha concluso l'aggiornamento, procedo
                    {
                        var copiaListaDizionario = positionsToCheck.ToList(); // Creare una copia per iterazioni sicure

                        foreach (var elemento in copiaListaDizionario)
                        {
                            inPositionGun = checker_monitoringPos.IsInPosition(elemento.Value, TCPCurrentPosition);

                            if (inPositionGun && elemento.Key > previousPoint && elemento.Key < previousPoint + 5) // Verifica posizione valida
                            {
                                #region Scrittura valori pistola a PLC

                                

                                #endregion

                                #region Colorazione lw

                                #endregion

                                // Aggiorna i riferimenti dei punti
                                previousPoint = elemento.Key;
                                currentIndex = elemento.Key;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex);
                }

                // Ritardo per ridurre la frequenza di esecuzione
                Thread.Sleep(positionCheckerThreadRefreshPeriod);
            }


        }

        #endregion

        #endregion

        #region Metodi per il debug

        /// <summary>
        /// Metodo che stoppa modalità teaching Point to Point
        /// </summary>
        public static void StopTeachingPTP()
        {
            byte state = new byte();
            DescPose pos = new DescPose();
            string guid_pos;
            string guid_gun_settings;
            GunSettings gunSettings;
            int id_gun_settings;

            // Se il robot è in TeachingMode, stoppo la modalità di teaching
            if (robot.IsInDragTeach(ref state) == 0 && state == 1)
            {
                robot.SetWebTPDStop();
                robot.DragTeachSwitch(0);

                // Salvo le posizioni registrate
                robot.GetActualTCPPose(flag, ref pos);

                RoundPositionDecimals(ref pos, 3);

                guid_pos = Guid.NewGuid().ToString();
                guid_gun_settings = Guid.NewGuid().ToString();
                id_gun_settings = UC_FullDragModePage.pointIndex + 1;

                gunSettings = new GunSettings(guid_gun_settings, guid_pos, id_gun_settings, 100, 100, 100, 100, 100, 1, RobotManager.applicationName);

                RobotManager.positionToSend = new PointPosition(guid_pos, pos, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"), "PTP", "", gunSettings); //$
                //positionsToSend.Add(point);
                positionsToSave.Add(RobotManager.positionToSend);
            }
        }

        /// <summary>
        /// Metodo che avvia modalità teaching Point to Point
        /// </summary>
        public static void StartTeachingPTP()
        {
            int type = 1;
            string name = "testTeach1";
            int period_ms = 2;
            UInt16 di_choose = 0;
            UInt16 do_choose = 0;

            robot.SetTPDParam(type, name, period_ms, di_choose, do_choose);

            robot.Mode(1);
            //Thread.Sleep(1000);
            robot.DragTeachSwitch(1);
            robot.SetTPDStart(type, name, period_ms, di_choose, do_choose);
        }

        /// <summary>
        /// Metodo che lancia Thread per salvataggio posizioni in LinearDragMode
        /// </summary>
        /// <param name="application"></param>
        public static void StartTeachingLineare()
        {
            int type = 1;
            string name = "testTeach1";
            int period_ms = 2;
            UInt16 di_choose = 0;
            UInt16 do_choose = 0;

            robot.SetTPDParam(type, name, period_ms, di_choose, do_choose);
            robot.Mode(1);
            Thread.Sleep(1000);
            robot.DragTeachSwitch(1);
            robot.SetTPDStart(type, name, period_ms, di_choose, do_choose);
            //robot.SetLoadWeight(0);
            // Faccio partire il thread
            timerCallback = new TimerCallback(ExecLinearDragMode);
            timer = new System.Threading.Timer(timerCallback, null, 100, velRec);
        }

        /// <summary>
        /// Metodo che stoppa modalità LinearDragMode
        /// </summary>
        /// <param name="application"></param>
        public static void StopTeachingLineare()
        {
            // Fermo il thread
            timer.Change(0, 0);

            byte state = new byte();

            // Se il robot è in TeachingMode, stoppo la modalità di teaching
            if (robot.IsInDragTeach(ref state) == 0 && state == 1)
            {
                robot.SetWebTPDStop();
                robot.DragTeachSwitch(0);
            }
        }

        /// <summary>
        /// Evento al premere del pulsante previous
        /// </summary>
        public static event EventHandler PointPositionAdded;

        /// <summary>
        /// Metodo che salva le posizioni nella lista di posizioni
        /// </summary>
        /// <param name="obj"></param>
        private static void ExecLinearDragMode(object obj)
        {
            // soglia per fare i controlli di in position 
            int threshold = 10; // 10 cm
            // Checker usato per fare in modo da non averne punti abbastanza simili tra loro risolvendo il
            // problema della linear drag mode
            PositionChecker checkerPos = new PositionChecker(threshold);
            // Quando i 2 punti sono simili sarà True
            bool isPositionMatch = false;

            // Dichiarazione ending point
            DescPose pos = new DescPose();
            // Oggetto gun settings
            GunSettings gunSettings;
            // ID dell'oggetto gun settings
            int id_gun_settings;
            // Guid posizione
            string guid_pos;
            // Guid gun settings della posizione
            string guid_gun_settings;

            // Salvo le posizioni registrate
            robot.GetActualTCPPose(flag, ref pos);
            // Arrotondamento a 3 cifre decimali
            RoundPositionDecimals(ref pos, 3);

            // Se ci sono dei punti salvati allora posso usare l'ultimo punto per fare il controllo inPosition
            if (positionsToSave.Count > 0)
                isPositionMatch = checkerPos.IsInPosition(pos, positionsToSave.Last().position);

            // Se le posizioni sono diverse le salvo e genero l'evento per la list view
            if (!isPositionMatch)
            {
                // Generazione del guid posizione
                guid_pos = Guid.NewGuid().ToString();
                // Generazione del guid gun settings
                guid_gun_settings = Guid.NewGuid().ToString();
                // Assegnazione del nuovo ID, l'ID del gun settings è uguale all'ID della posizione
                id_gun_settings = UC_FullDragModePage.pointIndex + 1;
                // Creazione oggetto gun settings
                gunSettings = new GunSettings(guid_gun_settings, guid_pos, id_gun_settings, 100, 100, 100, 100, 100, 1, applicationName);

                // Creazione del punto da inviare
                positionToSend = new PointPosition(guid_pos, pos, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"), "Linear", "", gunSettings);
                // Invoco l'evento per aggiungere la nuova riga nella list view positions
                PointPositionAdded?.Invoke(null, EventArgs.Empty);
                // Aggiungo la nuova posizione nella lista delle posizioni da salvare
                //positionsToSave.Add(new PointPosition(guid_pos, pos, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff"), "Linear", "", gunSettings));
                positionsToSave.Add(positionToSend);
            }
        }

        #endregion
    }
}
