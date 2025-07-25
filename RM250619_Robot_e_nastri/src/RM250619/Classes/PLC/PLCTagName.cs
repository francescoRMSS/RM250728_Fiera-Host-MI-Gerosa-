
namespace RM.src.RM250619.Classes.PLC
{
    /// <summary>
    /// Contiene i tag degli indirizzi PLC
    /// </summary>
    public class PLCTagName
    {
        #region Scrittura a PLC

        #region Robot

        /// <summary>
        /// Clock da scrivere ogni secondo (il clock su PLC gira ogni 2s)
        /// </summary>
        public const string LifeBit_out = "PLC1_" + "com_robot_1";

        /// <summary>
        /// Se il valore è 1, indica che la lista di lavoro del robot contiene almeno un programma
        /// </summary>
        public const string Hardware_Ready_To_Start = "PLC1_" + "com_robot_2";

        /// <summary>
        /// A 1 quando quando il robot lavora in modalità automatico
        /// </summary>
        public const string Automatic_Start = "PLC1_" + "com_robot_3";

        /// <summary>
        /// A 1 quando la catena è attiva
        /// </summary>
        public const string Chain_Enable = "PLC1_" + "com_robot_4";

        /// <summary>
        /// A 1 se è presente il programma nella memoria del Robot e quindi può essere inviato
        /// </summary>
        public const string Program_In_Memory = "PLC1_" + "com_robot_6";

        /// <summary>
        /// Stato della pinza
        /// </summary>
        public const string GripperStatusOut = "PLC1_" + "com_robot_11";

        /// <summary>
        /// A 1 quando il Robot è in errore ma ha potenza
        /// </summary>
        public const string System_error = "PLC1_" + "com_robot_15";

        /// <summary>
        /// A 1 quando il robot non ha potenza
        /// </summary>
        public const string Emergency = "PLC1_" + "com_robot_16";

        /// <summary>
        /// A 1 quando il Robot si trova nella safe area 
        /// </summary>
        public const string SafePos = "PLC1_" + "com_robot_17";

        /// <summary>
        /// A 1 quando il Robot finisce il ciclo automatico
        /// </summary>
        public const string Auto_Cycle_End = "PLC1_" + "com_robot_18";

        /// <summary>
        /// A 1 quando il Robot si trova in home position
        /// </summary>
        public const string Home_Pos = "PLC1_" + "com_robot_20";

        #endregion

        #region HMI

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public const string Cmd_Reset_Alarm = "PLC1_" + "com_robot_57";

        /// <summary>
        /// x axis actual position [mm]
        /// </summary>
        public const string x_actual_pos = "PLC1_" + "x_actual_pos";

        /// <summary>
        /// y axis actual position [mm]
        /// </summary>
        public const string y_actual_pos = "PLC1_" + "y_actual_pos";

        /// <summary>
        /// z axis actual position [mm]
        /// </summary>
        public const string z_actual_pos = "PLC1_" + "z_actual_pos";

        /// <summary>
        /// rx axis actual position [°]
        /// </summary>
        public const string rx_actual_pos = "PLC1_" + "rx_actual_pos";

        /// <summary>
        /// ry axis actual position [°]
        /// </summary>
        public const string ry_actual_pos = "PLC1_" + "ry_actual_pos";

        /// <summary>
        /// rz axis actual position [°]
        /// </summary>
        public const string rz_actual_pos = "PLC1_" + "rz_actual_pos";

        /// <summary>
        /// j1 actual position [°]
        /// </summary>
        public const string j1_actual_pos = "PLC1_" + "j1_actual_pos";

        /// <summary>
        /// j2 actual position [°]
        /// </summary>
        public const string j2_actual_pos = "PLC1_" + "j2_actual_pos";

        /// <summary>
        /// j3 actual position [°]
        /// </summary>
        public const string j3_actual_pos = "PLC1_" + "j3_actual_pos";

        /// <summary>
        /// j4 actual position [°]
        /// </summary>
        public const string j4_actual_pos = "PLC1_" + "j4_actual_pos";

        /// <summary>
        /// j5 actual position [°]
        /// </summary>
        public const string j5_actual_pos = "PLC1_" + "j5_actual_pos";

        /// <summary>
        /// j6 actual position [°]
        /// </summary>
        public const string j6_actual_pos = "PLC1_" + "j6_actual_pos";

        #endregion

        #endregion

        #region Lettura da PLC

        #region Robot

        /// <summary>
        /// Clock da leggere ogni secondo (su clock su PLC gira ogni 2s)
        /// </summary>
        public static string LifeBit_in = "PLC1_" + "com_robot_152";

        /// <summary>
        /// A 1 durante tutta la durante del ciclo
        /// </summary>
        public static string Start_Auto_Robot = "PLC1_" + "com_robot_153";

        /// <summary>
        /// 0-joint, 2-base, 4-tool, 8-workpiece
        /// </summary>
        public const string Jog_Ref_type = "PLC1_" + "com_robot_156";

        /// <summary>
        /// Jog speed set [%]
        /// </summary>
        public const string Jog_speed_set = "PLC1_" + "com_robot_157";

        /// <summary>
        /// 1=x / 2=y / 3=z / 4=rx / 5=ry / 6=rz
        /// </summary>
        public const string Axis_selection = "PLC1_" + "com_robot_158";

        /// <summary>
        /// x axis threshold [mm]
        /// </summary>
        public const string x_threshold = "PLC1_" + "com_robot_159";

        /// <summary>
        /// y axis threshold [mm]
        /// </summary>
        public const string y_threshold = "PLC1_" + "com_robot_160";

        /// <summary>
        /// z axis threshold [mm]
        /// </summary>
        public const string z_threshold = "PLC1_" + "com_robot_161";

        /// <summary>
        /// rx axis threshold [°]
        /// </summary>
        public const string rx_threshold = "PLC1_" + "com_robot_162";

        /// <summary>
        /// ry axis threshold [°]
        /// </summary>
        public const string ry_threshold = "PLC1_" + "com_robot_163";

        /// <summary>
        /// rz axis threshold [°]
        /// </summary>
        public const string rz_threshold = "PLC1_" + "com_robot_164";

        /// <summary>
        /// Spostamento della catena in mm
        /// </summary>
        public const string Chain_Pos = "PLC1_" + "com_robot_165";

        /// <summary>
        /// Enable robot from PLC
        /// </summary>
        public const string Enable = "PLC1_" + "com_robot_166";

        /// <summary>
        ///
        /// </summary>
        public const string MovePause = "PLC1_" + "com_robot_167";

        /// <summary>
        /// 0=Off, 1=auto, 2=man
        /// </summary>
        public const string Operating_Mode = "PLC1_" + "com_robot_168";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogX_pos = "PLC1_" + "com_robot_169";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogX_neg = "PLC1_" + "com_robot_170";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogY_pos = "PLC1_" + "com_robot_171";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogY_neg = "PLC1_" + "com_robot_172";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogZ_pos = "PLC1_" + "com_robot_173";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogZ_neg = "PLC1_" + "com_robot_174";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRX_pos = "PLC1_" + "com_robot_175";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRX_neg = "PLC1_" + "com_robot_176";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRY_pos = "PLC1_" + "com_robot_177";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRY_neg = "PLC1_" + "com_robot_178";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRZ_pos = "PLC1_" + "com_robot_179";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRZ_neg = "PLC1_" + "com_robot_180";

        #endregion

        #region HMI


        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Alarm_Presence = "PLC1_" + "com_robot_203";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Buzzer_On = "PLC1_" + "com_robot_204";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Manual_Mode = "PLC1_" + "com_robot_205";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Automatic_Mode = "PLC1_" + "com_robot_206";

        public static string PLC_Version_YYYY = "PLC1_" + "com_robot_211";

        public static string PLC_Version_MMDD = "PLC1_" + "com_robot_212";

        /// <summary>
        /// 0...1(bool)ALARM 1 - Emergency not OK
        /// </summary>
        public static string Alr_1 = "PLC1_" + "com_robot_213";

        /// <summary>
        /// 0...1(bool)ALARM 2 - Guards not OK
        /// </summary>
        public static string Alr_2 = "PLC1_" + "com_robot_214";

        /// <summary>
        /// 0...1(bool)ALARM 3 - 24Vdc gun thermal protection
        /// </summary>
        public static string Alr_3 = "PLC1_" + "com_robot_215";

        /// <summary>
        /// 0...1(bool)ALARM 4 - Error Feedback KM Emergency
        /// </summary>
        public static string Alr_4 = "PLC1_" + "com_robot_216";

        /// <summary>
        /// 0...1(bool)ALARM 5 - Error Feedback KM Gards
        /// </summary>
        public static string Alr_5 = "PLC1_" + "com_robot_217";

        /// <summary>
        /// 0...1(bool)ALARM 6 - Error F-Input modul
        /// </summary>
        public static string Alr_6 = "PLC1_" + "com_robot_218";

        /// <summary>
        /// 0...1(bool)ALARM 7 - Error F-Output modul
        /// </summary>
        public static string Alr_7 = "PLC1_" + "com_robot_219";

        /// <summary>
        /// 0...1(bool)ALARM 8 - Com Gun
        /// </summary>
        public static string Alr_8 = "PLC1_" + "com_robot_220";

        /// <summary>
        /// 0...1(bool)ALARM 9 - Emergency Mobile Panel not ok
        /// </summary>
        public static string Alr_9 = "PLC1_" + "com_robot_221";

        /// <summary>
        /// 0...1(bool)ALARM 10 -
        /// </summary>
        public static string Alr_10 = "PLC1_" + "com_robot_222";

        /// <summary>
        /// 0...1(bool)ALARM 11 -
        /// </summary>
        public static string Alr_11 = "PLC1_" + "com_robot_223";

        /// <summary>
        /// 0...1(bool)ALARM 12 -
        /// </summary>
        public static string Alr_12 = "PLC1_" + "com_robot_224";

        /// <summary>
        /// 0...1(bool)ALARM 13 -
        /// </summary>
        public static string Alr_13 = "PLC1_" + "com_robot_225";

        /// <summary>
        /// 0...1(bool)ALARM 14 -
        /// </summary>
        public static string Alr_14 = "PLC1_" + "com_robot_226";

        /// <summary>
        /// 0...1(bool)ALARM 15 -
        /// </summary>
        public static string Alr_15 = "PLC1_" + "com_robot_227";

        /// <summary>
        /// Errore di comunicazione con la pulsantiera
        /// </summary>
        public static string MobilePanel_CommError = "PLC1_" + "com_robot_234";

        /// <summary>
        /// Comando di apertura/chiusura pinza 1:Aperto|0:Chiuso
        /// </summary>
        public static string GripperStatusIn = "PLC1_" + "com_robot_235";

        #region DB17 (HMI)

        /// <summary>
        /// Comando per andare in home position 1:GoToHome|0:Normal state
        /// </summary>
        public static string StartApp = "PLC1_" + "hmi_1";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string StopApp = "PLC1_" + "hmi_2";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string GoHome = "PLC1_" + "hmi_3";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string SelectedFormat = "PLC1_" + "hmi_4";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string ResetAlarms = "PLC1_" + "hmi_5";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string PLCVersionYear = "PLC1_" + "hmi_6";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string PLCVersionMonth = "PLC1_" + "hmi_7";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string PLCVersionDay = "PLC1_" + "hmi_8";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string SelectedPallet = "PLC1_" + "hmi_9";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string openGrippers = "PLC1_" + "hmi_10";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string closeGrippers = "PLC1_" + "hmi_11";

        /// <summary>
        /// Comando per far partire il jog nastro 1:StartJogNastro|0:Normal state
        /// </summary>
        public static string jogNastro = "PLC1_" + "hmi_12";

        #endregion
       

        #endregion

        #endregion

    }
}
