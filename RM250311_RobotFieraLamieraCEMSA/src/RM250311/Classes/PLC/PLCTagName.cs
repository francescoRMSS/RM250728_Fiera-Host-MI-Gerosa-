
namespace RM.src.RM250311.Classes.PLC
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
        /// Libero
        /// </summary>
        public const string spare_2_to_write = "PLC1_" + "com_robot_5";

        /// <summary>
        /// A 1 se è presente il programma nella memoria del Robot e quindi può essere inviato
        /// </summary>
        public const string Program_In_Memory = "PLC1_" + "com_robot_6";

        /// <summary>
        /// Posizione 1 del Robot per eseguire lavaggio
        /// </summary>
        public const string Robot_Pos_Wash_1 = "PLC1_" + "com_robot_7";

        /// <summary>
        /// A 1 durante l'esecuzione del lavaggio nella posizione 1
        /// </summary>
        public const string Robot_Washing_1 = "PLC1_" + "com_robot_8";

        /// <summary>
        /// Posizione 2 del Robot per eseguire lavaggio
        /// </summary>
        public const string Robot_Pos_Wash_2 = "PLC1_" + "com_robot_9";

        /// <summary>
        /// A 1 durante l'esecuzione del lavaggio nella posizione 2
        /// </summary>
        public const string Robot_Washing_2 = "PLC1_" + "com_robot_10";

        /// <summary>
        /// Stato della pinza
        /// </summary>
        public const string GripperStatusOut = "PLC1_" + "com_robot_11";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_4_to_write = "PLC1_" + "com_robot_12";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_5_to_write = "PLC1_" + "com_robot_13";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_6_to_write = "PLC1_" + "com_robot_14";

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
        /// 1 = comando a pistola
        /// </summary>
        public const string Start_Gun = "PLC1_" + "com_robot_19";

        /// <summary>
        /// A 1 quando il Robot si trova in home position
        /// </summary>
        public const string Home_Pos = "PLC1_" + "com_robot_20";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_8_to_write = "PLC1_" + "com_robot_21";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_9_to_write = "PLC1_" + "com_robot_22";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_10_to_write = "PLC1_" + "com_robot_23";

        /// <summary>
        /// kV (range 0 - 1000)
        /// </summary>
        public const string Kv = "PLC1_" + "com_robot_24";

        /// <summary>
        /// Polvere (range 0 - 500)
        /// </summary>
        public const string Bar_Per_100_POLVERE = "PLC1_" + "com_robot_25";

        /// <summary>
        /// Dosaggio (range 0 - 500)
        /// </summary>
        public const string Bar_Per_100_DOSAGGIO = "PLC1_" + "com_robot_26";

        /// <summary>
        /// Aria (range 0 - 500)
        /// </summary>
        public const string Bar_Per_100_ARIA = "PLC1_" + "com_robot_27";

        /// <summary>
        /// Microampere (range 0 - 1000)
        /// </summary>
        public const string UA = "PLC1_" + "com_robot_28";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_11_to_write = "PLC1_" + "com_robot_29";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_12_to_write = "PLC1_" + "com_robot_30";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_13_to_write = "PLC1_" + "com_robot_31";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_14_to_write = "PLC1_" + "com_robot_32";

        /// <summary>
        /// Libero
        /// </summary>
        public const string spare_15_to_write = "PLC1_" + "com_robot_33";

        #endregion

        #region HMI

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public const string Cmd_Start_Lavaggio = "PLC1_" + "com_robot_34";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public const string Cmd_PuliziaBox = "PLC1_" + "com_robot_35";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public const string Unit_Of_Measure = "PLC1_" + "com_robot_36";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public const string Cmd_Reset_Alarm = "PLC1_" + "com_robot_37";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public const string Gun_On_Off = "PLC1_" + "com_robot_38";

        public const string Apply_Autotune_Frequency = "PLC1_" + "com_robot_39";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public const string Start_Autotune = "PLC1_" + "com_robot_40";

        /// <summary>
        /// 0...1
        /// </summary>
        public const string Mode_Tribo_Corona = "PLC1_" + "com_robot_41";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_Topfreinigung = "PLC1_" + "com_robot_42";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_SysReinigeng = "PLC1_" + "com_robot_43";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_Pulver_Low_Pression = "PLC1_" + "com_robot_44";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_Dosage_Low_Pression = "PLC1_" + "com_robot_45";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_Low_Pression = "PLC1_" + "com_robot_46";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_Pulver_High_Pression = "PLC1_" + "com_robot_47";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_Dosage_High_Pression = "PLC1_" + "com_robot_48";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Air_High_Pression = "PLC1_" + "com_robot_49";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Lim_Pulver_Air = "PLC1_" + "com_robot_50";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Lim_Dosage_Air = "PLC1_" + "com_robot_51";

        /// <summary>
        /// 0...500
        /// </summary>
        public const string Lim_Air = "PLC1_" + "com_robot_52";

        /// <summary>
        /// kV (range 0 - 1000)
        /// </summary>
        public const string HMI_Kv = "PLC1_" + "com_robot_53";

        /// <summary>
        /// Polvere (range 0 - 500)
        /// </summary>
        public const string HMI_Bar_Per_100_POLVERE = "PLC1_" + "com_robot_54";

        /// <summary>
        /// Dosaggio (range 0 - 500
        /// </summary>
        public const string HMI_Bar_Per_100_DOSAGGIO = "PLC1_" + "com_robot_55";

        /// <summary>
        /// Aria (range 0 - 500)
        /// </summary>
        public const string HMI_Bar_Per_100_ARIA = "PLC1_" + "com_robot_56";

        /// <summary>
        /// Microampere (range 0 - 1000)
        /// </summary>
        public const string HMI_UA = "PLC1_" + "com_robot_57";

        /// <summary>
        /// 360....420
        /// </summary>
        public const string AutotuneFrequency = "PLC1_" + "com_robot_58";

        /// <summary>
        /// 10..50
        /// </summary>
        public const string Limit_UA_Autotune = "PLC1_" + "com_robot_59";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_9_to_write = "PLC1_" + "com_robot_60";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_10_to_write = "PLC1_" + "com_robot_61";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_11_to_write = "PLC1_" + "com_robot_62";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_12_to_write = "PLC1_" + "com_robot_63";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_13_to_write = "PLC1_" + "com_robot_64";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_14_to_write = "PLC1_" + "com_robot_65";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_15_to_write = "PLC1_" + "com_robot_66";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_16_to_write = "PLC1_" + "com_robot_67";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_17_to_write = "PLC1_" + "com_robot_68";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_18_to_write = "PLC1_" + "com_robot_69";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_19_to_write = "PLC1_" + "com_robot_70";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_20_to_write = "PLC1_" + "com_robot_71";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_21_to_write = "PLC1_" + "com_robot_72";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_22_to_write = "PLC1_" + "com_robot_73";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_23_to_write = "PLC1_" + "com_robot_74";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_24_to_write = "PLC1_" + "com_robot_75";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_25_to_write = "PLC1_" + "com_robot_76";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_26_to_write = "PLC1_" + "com_robot_77";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_27_to_write = "PLC1_" + "com_robot_78";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_28_to_write = "PLC1_" + "com_robot_79";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_29_to_write = "PLC1_" + "com_robot_80";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_30_to_write = "PLC1_" + "com_robot_81";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_31_to_write = "PLC1_" + "com_robot_82";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_32_to_write = "PLC1_" + "com_robot_83";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_33_to_write = "PLC1_" + "com_robot_84";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_34_to_write = "PLC1_" + "com_robot_85";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_35_to_write = "PLC1_" + "com_robot_86";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_36_to_write = "PLC1_" + "com_robot_87";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_37_to_write = "PLC1_" + "com_robot_88";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_38_to_write = "PLC1_" + "com_robot_89";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_39_to_write = "PLC1_" + "com_robot_90";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_40_to_write = "PLC1_" + "com_robot_91";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_41_to_write = "PLC1_" + "com_robot_92";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_42_to_write = "PLC1_" + "com_robot_93";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_43_to_write = "PLC1_" + "com_robot_94";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_44_to_write = "PLC1_" + "com_robot_95";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_45_to_write = "PLC1_" + "com_robot_96";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_46_to_write = "PLC1_" + "com_robot_97";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_47_to_write = "PLC1_" + "com_robot_98";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_48_to_write = "PLC1_" + "com_robot_99";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_49_to_write = "PLC1_" + "com_robot_100";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Free_50_to_write = "PLC1_" + "com_robot_101";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_0_to_write = "PLC1_" + "com_robot_102";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_1_to_write = "PLC1_" + "com_robot_103";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_2_to_write = "PLC1_" + "com_robot_104";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_3_to_write = "PLC1_" + "com_robot_105";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_4_to_write = "PLC1_" + "com_robot_106";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_5_to_write = "PLC1_" + "com_robot_107";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_6_to_write = "PLC1_" + "com_robot_108";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_7_to_write = "PLC1_" + "com_robot_109";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_8_to_write = "PLC1_" + "com_robot_110";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_9_to_write = "PLC1_" + "com_robot_111";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_10_to_write = "PLC1_" + "com_robot_112";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_11_to_write = "PLC1_" + "com_robot_113";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_12_to_write = "PLC1_" + "com_robot_114";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_13_to_write = "PLC1_" + "com_robot_115";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_14_to_write = "PLC1_" + "com_robot_116";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_15_to_write = "PLC1_" + "com_robot_117";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_16_to_write = "PLC1_" + "com_robot_118";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_17_to_write = "PLC1_" + "com_robot_119";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_18_to_write = "PLC1_" + "com_robot_120";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_19_to_write = "PLC1_" + "com_robot_121";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_20_to_write = "PLC1_" + "com_robot_122";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_21_to_write = "PLC1_" + "com_robot_123";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_22_to_write = "PLC1_" + "com_robot_124";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_23_to_write = "PLC1_" + "com_robot_125";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_24_to_write = "PLC1_" + "com_robot_126";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_25_to_write = "PLC1_" + "com_robot_127";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_26_to_write = "PLC1_" + "com_robot_128";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_27_to_write = "PLC1_" + "com_robot_129";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_28_to_write = "PLC1_" + "com_robot_130";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_29_to_write = "PLC1_" + "com_robot_131";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_30_to_write = "PLC1_" + "com_robot_132";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_31_to_write = "PLC1_" + "com_robot_133";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_32_to_write = "PLC1_" + "com_robot_134";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_33_to_write = "PLC1_" + "com_robot_135";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_34_to_write = "PLC1_" + "com_robot_136";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_35_to_write = "PLC1_" + "com_robot_137";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_36_to_write = "PLC1_" + "com_robot_138";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_37_to_write = "PLC1_" + "com_robot_139";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_38_to_write = "PLC1_" + "com_robot_140";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_39_to_write = "PLC1_" + "com_robot_141";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_40_to_write = "PLC1_" + "com_robot_142";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_41_to_write = "PLC1_" + "com_robot_143";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_42_to_write = "PLC1_" + "com_robot_144";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_43_to_write = "PLC1_" + "com_robot_145";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_44_to_write = "PLC1_" + "com_robot_146";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_45_to_write = "PLC1_" + "com_robot_147";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_46_to_write = "PLC1_" + "com_robot_148";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_47_to_write = "PLC1_" + "com_robot_149";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_48_to_write = "PLC1_" + "com_robot_150";

        /// <summary>
        /// Libero
        /// </summary>
        public const string Array_Spare_49_to_write = "PLC1_" + "com_robot_151";

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
        /// A 1 quando parte il lavaggio in posizione 1 (1s ON)
        /// </summary>
        public static string Start_Wash_1 = "PLC1_" + "com_robot_154";

        /// <summary>
        /// A 1 quando parte il lavaggio in posizione 2 (1s ON)
        /// </summary>
        public static string Start_Wash_2 = "PLC1_" + "com_robot_155";

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
        /// 0=Off, 1=auto, 2=man
        /// </summary>
        public const string Operating_Mode = "PLC1_" + "com_robot_167";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogX_pos = "PLC1_" + "com_robot_168";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogX_neg = "PLC1_" + "com_robot_169";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogY_pos = "PLC1_" + "com_robot_170";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogY_neg = "PLC1_" + "com_robot_171";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogZ_pos = "PLC1_" + "com_robot_172";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogZ_neg = "PLC1_" + "com_robot_173";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRX_pos = "PLC1_" + "com_robot_174";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRX_neg = "PLC1_" + "com_robot_175";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRY_pos = "PLC1_" + "com_robot_176";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRY_neg = "PLC1_" + "com_robot_177";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRZ_pos = "PLC1_" + "com_robot_178";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string JogRZ_neg = "PLC1_" + "com_robot_179";

        #endregion

        #region HMI

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Vis_Lavaggio_In_Corso = "PLC1_" + "com_robot_180";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Vis_PuliziaBox = "PLC1_" + "com_robot_181";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Alarm_Presence = "PLC1_" + "com_robot_182";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Buzzer_On = "PLC1_" + "com_robot_183";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Manual_Mode = "PLC1_" + "com_robot_184";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string Automatic_Mode = "PLC1_" + "com_robot_185";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string GUN_1_SPRAY = "PLC1_" + "com_robot_186";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string GUN_1_Autotune_Start = "PLC1_" + "com_robot_187";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string GUN_1_Autotune_Err = "PLC1_" + "com_robot_188";

        /// <summary>
        /// 0...1(bool)
        /// </summary>
        public static string GUN_1_Autotune_Ok = "PLC1_" + "com_robot_189";

        public static string PLC_Version_YYYY = "PLC1_" + "com_robot_190";

        public static string PLC_Version_MMDD = "PLC1_" + "com_robot_191";

        /// <summary>
        /// 0...1(bool)ALARM 1 - Emergency not OK
        /// </summary>
        public static string Alr_1 = "PLC1_" + "com_robot_192";

        /// <summary>
        /// 0...1(bool)ALARM 2 - Guards not OK
        /// </summary>
        public static string Alr_2 = "PLC1_" + "com_robot_193";

        /// <summary>
        /// 0...1(bool)ALARM 3 - 24Vdc gun thermal protection
        /// </summary>
        public static string Alr_3 = "PLC1_" + "com_robot_194";

        /// <summary>
        /// 0...1(bool)ALARM 4 - Error Feedback KM Emergency
        /// </summary>
        public static string Alr_4 = "PLC1_" + "com_robot_195";

        /// <summary>
        /// 0...1(bool)ALARM 5 - Error Feedback KM Gards
        /// </summary>
        public static string Alr_5 = "PLC1_" + "com_robot_196";

        /// <summary>
        /// 0...1(bool)ALARM 6 - Error F-Input modul
        /// </summary>
        public static string Alr_6 = "PLC1_" + "com_robot_197";

        /// <summary>
        /// 0...1(bool)ALARM 7 - Error F-Output modul
        /// </summary>
        public static string Alr_7 = "PLC1_" + "com_robot_198";

        /// <summary>
        /// 0...1(bool)ALARM 8 - Com Gun
        /// </summary>
        public static string Alr_8 = "PLC1_" + "com_robot_199";

        /// <summary>
        /// 0...1(bool)ALARM 9 - Emergency Mobile Panel not ok
        /// </summary>
        public static string Alr_9 = "PLC1_" + "com_robot_200";

        /// <summary>
        /// 0...1(bool)ALARM 10 -
        /// </summary>
        public static string Alr_10 = "PLC1_" + "com_robot_201";

        /// <summary>
        /// 0...1(bool)ALARM 11 -
        /// </summary>
        public static string Alr_11 = "PLC1_" + "com_robot_202";

        /// <summary>
        /// 0...1(bool)ALARM 12 -
        /// </summary>
        public static string Alr_12 = "PLC1_" + "com_robot_203";

        /// <summary>
        /// 0...1(bool)ALARM 13 -
        /// </summary>
        public static string Alr_13 = "PLC1_" + "com_robot_204";

        /// <summary>
        /// 0...1(bool)ALARM 14 -
        /// </summary>
        public static string Alr_14 = "PLC1_" + "com_robot_205";

        /// <summary>
        /// 0...1(bool)ALARM 15 -
        /// </summary>
        public static string Alr_15 = "PLC1_" + "com_robot_206";

        /// <summary>
        /// bar 100==1bar---500=5 BAR
        /// </summary>
        public static string GUN_1_ACT_POLVERE = "PLC1_" + "com_robot_207";

        /// <summary>
        /// bar 100==1bar---500=5 BAR
        /// </summary>
        public static string GUN_1_ACT_DOSAGGIO = "PLC1_" + "com_robot_208";

        /// <summary>
        /// UA 0-1000---25UA==250
        /// </summary>
        public static string GUN_1_RET_UA = "PLC1_" + "com_robot_209";

        /// <summary>
        /// UA 0-1000---25UA==250
        /// </summary>
        public static string GUN_1_RET_UA_TRIBO = "PLC1_" + "com_robot_210";

        /// <summary>
        /// kv 0-1000---25KV==250
        /// </summary>
        public static string GUN_1_RET_KV = "PLC1_" + "com_robot_211";

        /// <summary>
        /// 360...420
        /// </summary>
        public static string GUN_1_RET_AUTOTUNING_FREQUENCY = "PLC1_" + "com_robot_212";

        /// <summary>
        /// Errore di comunicazione con la pulsantiera
        /// </summary>
        public static string MobilePanel_CommError = "PLC1_" + "com_robot_213";

        /// <summary>
        /// Stato pinza
        /// </summary>
        public static string GripperStatusIn = "PLC1_" + "com_robot_214";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_26_to_read = "PLC1_" + "com_robot_215";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_27_to_read = "PLC1_" + "com_robot_216";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_28_to_read = "PLC1_" + "com_robot_217";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_29_to_read = "PLC1_" + "com_robot_218";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_30_to_read = "PLC1_" + "com_robot_219";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_31_to_read = "PLC1_" + "com_robot_220";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_32_to_read = "PLC1_" + "com_robot_221";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_33_to_read = "PLC1_" + "com_robot_222";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_34_to_read = "PLC1_" + "com_robot_223";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_35_to_read = "PLC1_" + "com_robot_224";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_36_to_read = "PLC1_" + "com_robot_225";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_37_to_read = "PLC1_" + "com_robot_226";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_38_to_read = "PLC1_" + "com_robot_227";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_39_to_read = "PLC1_" + "com_robot_228";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_40_to_read = "PLC1_" + "com_robot_229";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_41_to_read = "PLC1_" + "com_robot_230";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_42_to_read = "PLC1_" + "com_robot_231";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_43_to_read = "PLC1_" + "com_robot_232";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_44_to_read = "PLC1_" + "com_robot_233";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_45_to_read = "PLC1_" + "com_robot_234";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_46_to_read = "PLC1_" + "com_robot_235";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_47_to_read = "PLC1_" + "com_robot_236";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_48_to_read = "PLC1_" + "com_robot_237";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_49_to_read = "PLC1_" + "com_robot_238";

        /// <summary>
        /// Libero
        /// </summary>
        public static string Free_50_to_read = "PLC1_" + "com_robot_239";

        #endregion

        #endregion

    }
}
