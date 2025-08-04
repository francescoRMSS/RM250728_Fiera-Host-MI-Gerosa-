
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

        #region spare

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_1_to_write = "PLC1_" + "com_robot_4";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_2_to_write = "PLC1_" + "com_robot_5";

        #endregion

        /// <summary>
        /// A 1 se è presente il programma nella memoria del Robot e quindi può essere inviato
        /// </summary>
        public const string Program_In_Memory = "PLC1_" + "com_robot_6";

        #region spare

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_3_to_write = "PLC1_" + "com_robot_7";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_7_to_write = "PLC1_" + "com_robot_8";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_16_to_write = "PLC1_" + "com_robot_9";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_17_to_write = "PLC1_" + "com_robot_10";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_24_to_write = "PLC1_" + "com_robot_11";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_4_to_write = "PLC1_" + "com_robot_12";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_5_to_write = "PLC1_" + "com_robot_13";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_6_to_write = "PLC1_" + "com_robot_14";

        #endregion

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

        #region spare

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_18_to_write = "PLC1_" + "com_robot_19";

        #endregion

        /// <summary>
        /// A 1 quando il Robot si trova in zona di home
        /// </summary>
        public const string ACT_Zone_Home_inPos = "PLC1_" + "com_robot_20";

        /// <summary>
        /// A 1 quando il Robot si trova in zona di pick
        /// </summary>
        public const string ACT_Zone_Pick_inPos = "PLC1_" + "com_robot_21";

        /// <summary>
        /// A 1 quando il Robot si trova in zona di place
        /// </summary>
        public const string ACT_Zone_Place_inPos = "PLC1_" + "com_robot_22";

        #region spare

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_10_to_write = "PLC1_" + "com_robot_23";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_19_to_write = "PLC1_" + "com_robot_24";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_20_to_write = "PLC1_" + "com_robot_25";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_21_to_write = "PLC1_" + "com_robot_26";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_22_to_write = "PLC1_" + "com_robot_27";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_23_to_write = "PLC1_" + "com_robot_28";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_11_to_write = "PLC1_" + "com_robot_29";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_12_to_write = "PLC1_" + "com_robot_30";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_13_to_write = "PLC1_" + "com_robot_31";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_14_to_write = "PLC1_" + "com_robot_32";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_15_to_write = "PLC1_" + "com_robot_33";

        #endregion

        #region array di spare
        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_0_to_write = "PLC1_" + "com_robot_34";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_1_to_write = "PLC1_" + "com_robot_35";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_2_to_write = "PLC1_" + "com_robot_36";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_3_to_write = "PLC1_" + "com_robot_37";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_4_to_write = "PLC1_" + "com_robot_38";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_5_to_write = "PLC1_" + "com_robot_39";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_6_to_write = "PLC1_" + "com_robot_40";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_7_to_write = "PLC1_" + "com_robot_41";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_8_to_write = "PLC1_" + "com_robot_42";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_9_to_write = "PLC1_" + "com_robot_43";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_10_to_write = "PLC1_" + "com_robot_44";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_11_to_write = "PLC1_" + "com_robot_45";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_12_to_write = "PLC1_" + "com_robot_46";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_13_to_write = "PLC1_" + "com_robot_47";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_14_to_write = "PLC1_" + "com_robot_48";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_15_to_write = "PLC1_" + "com_robot_49";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_16_to_write = "PLC1_" + "com_robot_50";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_17_to_write = "PLC1_" + "com_robot_51";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_18_to_write = "PLC1_" + "com_robot_52";

        /// <summary>
        /// spare
        /// </summary>
        public const string array_spare_19_to_write = "PLC1_" + "com_robot_53";

        #endregion

        #endregion

        #region HMI

        #region spare

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_hmi_24_to_write = "PLC1_" + "com_robot_54";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_hmi_25_to_write = "PLC1_" + "com_robot_55";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_hmi_26_to_write = "PLC1_" + "com_robot_56";

        /// <summary>
        /// spare
        /// </summary>
        public const string spare_hmi_50_to_write = "PLC1_" + "com_robot_57";

        #endregion

        /// <summary>
        /// Numero di step main cycle
        /// </summary>
        public const string ACT_Step_MainCycle = "PLC1_" + "com_robot_58";

        /// <summary>
        /// Numero di step ciclo di pick
        /// </summary>
        public const string ACT_Step_Cycle_Pick = "PLC1_" + "com_robot_59";

        /// <summary>
        /// Numero di step ciclo di place
        /// </summary>
        public const string ACT_Step_Cycle_Place = "PLC1_" + "com_robot_60";

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

        #region spare

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_to_read_50 = "PLC1_" + "com_robot_154";

        #endregion

        /// <summary>
        /// spare
        /// </summary>
        public static string CMD_OverrideAuto = "PLC1_" + "com_robot_155";

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
        /// A 1 per far girare il main
        /// </summary>
        public static string CMD_StartApp = "PLC1_" + "com_robot_201";

        /// <summary>
        /// A 1 per fermare il main
        /// </summary>
        public static string CMD_StopApp = "PLC1_" + "com_robot_202";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Alarm_Presence = "PLC1_" + "com_robot_203";

        /// <summary>
        /// A 1 per avvia routine per tornare in home
        /// </summary>
        public static string CMD_GoHome = "PLC1_" + "com_robot_204";

        /// <summary>
        /// A 1 quando il robot è in manuale
        /// </summary>
        public static string Manual_Mode = "PLC1_" + "com_robot_205";

        /// <summary>
        /// A 1 quando il robot è in automatico
        /// </summary>
        public static string Automatic_Mode = "PLC1_" + "com_robot_206";

        /// <summary>
        /// Formato selezionato
        /// </summary>
        public static string CMD_SelectedFormat = "PLC1_" + "com_robot_207";

        /// <summary>
        /// A 1 per resettare gli allarmi
        /// </summary>
        public static string CMD_ResetAlarms = "PLC1_" + "com_robot_208";

        /// <summary>
        ///  Pallet selezionato
        /// </summary>
        public static string CMD_SelectedPallet = "PLC1_" + "com_robot_209";

        /// <summary>
        /// Livello del pallet
        /// </summary>
        public static string CMD_Layer_Pallet = "PLC1_" + "com_robot_210";

        /// <summary>
        /// Indice della scatola
        /// </summary>
        public static string CMD_Index_Box = "PLC1_" + "com_robot_211";

        /// <summary>
        /// Scatola ruotata/non ruotata
        /// </summary>
        public static string CMD_Box_Rotate_180 = "PLC1_" + "com_robot_212";

        #region spare

        /// <summary>
        /// spare
        /// </summary>
        public static string spare = "PLC1_" + "com_robot_213";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_1 = "PLC1_" + "com_robot_214";

        #endregion

        /// <summary>
        /// A 1 per consenso di pick
        /// </summary>
        public static string Enable_To_Pick = "PLC1_" + "com_robot_215";

        /// <summary>
        /// A 1 per consenso di place
        /// </summary>
        public static string Enable_To_Place = "PLC1_" + "com_robot_216";

        /// <summary>
        /// 0...1(bool)ALARM 5 - Error Feedback KM Gards
        /// </summary>
        public static string Alr_5 = "PLC1_" + "com_robot_217";

        /// <summary>
        /// 0...1(bool)ALARM 6 - Error F-Input modul
        /// </summary>
        public static string Alr_6 = "PLC1_" + "com_robot_218";

        /// <summary>
        /// A 1 per eseguire pick
        /// </summary>
        public static string CMD_Pick = "PLC1_" + "com_robot_219";

        /// <summary>
        /// A 1 per eseguire place
        /// </summary>
        public static string CMD_Place = "PLC1_" + "com_robot_220";

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
        /// A 1 per apertura manuale pinze
        /// </summary>
        public static string CMD_MAN_openGrippers = "PLC1_" + "com_robot_228";

        /// <summary>
        /// A 1 per chiusura manuale pinze
        /// </summary>
        public static string CMD_MAN_closeGrippers = "PLC1_" + "com_robot_229";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_64 = "PLC1_" + "com_robot_230";

        /// <summary>
        /// A 1 per registrare il punto
        /// </summary>
        public static string CMD_RegisterPoint = "PLC1_" + "com_robot_231";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_62 = "PLC1_" + "com_robot_232";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_63 = "PLC1_" + "com_robot_233";

        /// <summary>
        /// Errore di comunicazione con la pulsantiera
        /// </summary>
        public static string MobilePanel_CommError = "PLC1_" + "com_robot_234";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_51 = "PLC1_" + "com_robot_235";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_26 = "PLC1_" + "com_robot_236";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_27 = "PLC1_" + "com_robot_237";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_28 = "PLC1_" + "com_robot_238";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_29 = "PLC1_" + "com_robot_239";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_30 = "PLC1_" + "com_robot_240";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_31 = "PLC1_" + "com_robot_241";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_32 = "PLC1_" + "com_robot_242";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_33 = "PLC1_" + "com_robot_243";

        /// <summary>
        /// spare
        /// </summary>
        public static string spare_34 = "PLC1_" + "com_robot_244";

        /// <summary>
        /// xOffset punto di pick
        /// </summary>
        public static string OFFSET_Pick_X = "PLC1_" + "com_robot_245";

        /// <summary>
        /// yOffset punto di pick
        /// </summary>
        public static string OFFSET_Pick_Y = "PLC1_" + "com_robot_246";

        /// <summary>
        /// zOffset punto di pick
        /// </summary>
        public static string OFFSET_Pick_Z = "PLC1_" + "com_robot_247";

        /// <summary>
        /// xOffset punto di place
        /// </summary>
        public static string OFFSET_Place_X = "PLC1_" + "com_robot_248";

        /// <summary>
        /// yOffset punto di place
        /// </summary>
        public static string OFFSET_Place_Y = "PLC1_" + "com_robot_249";

        /// <summary>
        /// zOffset punto di place
        /// </summary>
        public static string OFFSET_Place_Z = "PLC1_" + "com_robot_250";


        #endregion

        #endregion

    }
}
