using fairino;
using System.Collections.Generic;

namespace RM.src.RM250619.Classes.FR20
{
    /// <summary>
    /// Rappresenta un tool pronto per essere inserito nella lista di tool
    /// </summary>
    struct ToolStruct
    {
        public int id; // ID Tool [0 = tool di default del robot], è quello che viene scritto al robot
        public string name; // Nome da usare per cambiare il tool del robot in quello scelto
        public DescPose pose; // Pose che identifica i punti calcolati dalla matrice di roto-traslazione dal robot
        public int type;
        public int install;
    }

    /// <summary>
    /// Gstisce i tool del robot
    /// </summary>
    public class Tools
    {
        private List<ToolStruct> _tools;
        private Robot _robot;
        public int currentTool = -1;

        public Tools(Robot robot)
        {
            _robot = robot;
            _tools = new List<ToolStruct>();

            InitList();
        }

        private void InitList()
        {
            ToolStruct tool1 = new ToolStruct
            {
                id = 1,
                name = "tVentosa",
                pose = new DescPose(0, 0, 50, 0, 0, 0),
                type = 0,
                install = 0
            };
            _tools.Add(tool1);
        }

        private ToolStruct? ReadToolData(int toolId)
        {
            ToolStruct _data = new ToolStruct();

            foreach (ToolStruct _struct in _tools)
            {
                if (_struct.id == toolId)
                {
                    _data.id = _struct.id;
                    _data.name = _struct.name;
                    _data.pose = _struct.pose;

                    return _data;
                }
            }

            return null;
        }

        private ToolStruct? ReadToolData(string toolName)
        {
            ToolStruct _data = new ToolStruct();

            foreach (ToolStruct _struct in _tools)
            {
                if (_struct.name.Equals(toolName))
                {
                    _data.id = _struct.id;
                    _data.name = _struct.name;
                    _data.pose = _struct.pose;

                    return _data;
                }
            }

            return null;
        }

        /// <summary>
        /// Modifica il tool in uso del robot e controlla che sia stato cambiato effettivamente
        /// </summary>
        /// <param name="toolId"></param>
        /// <returns></returns>
        public int ChangeRobotTool(int toolId)
        {
            int _checkNewTool = -1;
            ToolStruct? _data;

            if (toolId < 0) // Frame non valido
                return 0;

            _data = ReadToolData(toolId);
            if (_data == null) // ID Frame non trovato nella lista
                return 1;

            if (_data.Value.id == currentTool)
                return 1;

            _robot.SetToolCoord(toolId, _data.Value.pose, _data.Value.type, _data.Value.install);
            _robot.GetActualTCPNum(0, ref _checkNewTool);

            if (_checkNewTool != toolId)    // ID di risposta diverso da ID settato
                return 3;

            RobotManager.user = toolId;
            currentTool = toolId;
            return 10;
        }

        /// <summary>
        /// Modifica il tool in uso del robot e controlla che sia stato cambiato effettivamente
        /// </summary>
        /// <param name="toolName"></param>
        /// <returns></returns>
        public int ChangeRobotTool(string toolName)
        {
            int _checkNewTool = -1;
            ToolStruct? _data;

            if (toolName.Length == 0 || toolName == null) // Frame non valido
                return 0;

            _data = ReadToolData(toolName);
            if (_data == null) // ID Frame non trovato nella lista
                return 1;

            if (_data.Value.id == currentTool)
                return 2;

            _robot.SetToolCoord(_data.Value.id, _data.Value.pose, _data.Value.type, _data.Value.install);
            _robot.GetActualTCPNum(0, ref _checkNewTool);

            if (_checkNewTool != _data.Value.id)    // ID di risposta diverso da ID settato
                return 3;

            RobotManager.user = _data.Value.id;
            currentTool = _data.Value.id;
            return 10;
        }

        public bool IsErrorBlocking(int errNum)
        {
            switch (errNum)
            {
                case 0:
                    return true;
                case 1:
                    return true;
                case 2:
                    return false;
                case 3:
                    return true;
                case 10:
                    return false;
                default:
                    return true;
            }
        }

        public string GetErrorCode(int errNum)
        {
            string errCode = "";

            switch (errNum)
            {
                case 0:
                    errCode = "Tool ID minore di 0";
                    break;
                case 1:
                    errCode = "Tool ID o nome Tool non trovato";
                    break;
                case 2:
                    errCode = "Tool già impostato";
                    break;
                case 3:
                    errCode = "Tool impostato diverso dal frame desiderato";
                    break;
                case 10:
                    errCode = "Tool modificato correttamente";
                    break;
                default:
                    errCode = "Error number non trovato";
                    break;
            }

            return errCode;
        }
    }
}
