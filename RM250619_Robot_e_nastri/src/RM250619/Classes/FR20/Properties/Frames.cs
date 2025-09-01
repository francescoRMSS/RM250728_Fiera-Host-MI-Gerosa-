using fairino;
using System.Collections.Generic;

namespace RM.src.RM250619.Classes.FR20
{
    /// <summary>
    /// Rappresenta un frame pronto per essere inserito nella lista di frame
    /// </summary>
    struct FrameStruct
    {
        public int id; // ID frame [0 = frame di default del robot], è quello che viene scritto al robot
        public string name; // Nome da usare per cambiare il frame del robot in quello scelto
        public DescPose pose; // Pose che identifica i punti calcolati dalla matrice di roto-traslazione dal robot
    }

    /// <summary>
    /// Gestisce i frame del robot
    /// </summary>
    public class Frames
    {
        private List<FrameStruct> _frames;
        private Robot _robot;
        public int currentFrame = -1;

        public Frames(Robot robot)
        {
            _robot = robot;
            _frames = new List<FrameStruct>();

            InitList();
        }

        private void InitList()
        {
            FrameStruct frame1 = new FrameStruct
            {
                id = 1,
                name = "frNastro",
                pose = new DescPose(-830.117, 207.966, -620.278, 0.006, 0.009, -147.102)
            };
            _frames.Add(frame1);

            FrameStruct frame2 = new FrameStruct
            {
                id = 2,
                name = "frPallet1",
                pose = new DescPose(-815.980, -719.498, -497.565, 0.006, 0.003, 122.472)
            };
            _frames.Add(frame2);

            FrameStruct frame3 = new FrameStruct
            {
                id = 3,
                name = "frPallet2",
                pose = new DescPose(0, 0, 0, 0, 0, 0)
            };
            _frames.Add(frame3);
        }

        private FrameStruct? ReadFrameData(int frameId)
        {
            FrameStruct _data = new FrameStruct();

            foreach(FrameStruct _struct in _frames)
            {
                if(_struct.id == frameId)
                {
                    _data.id = _struct.id;
                    _data.name = _struct.name;
                    _data.pose = _struct.pose;

                    return _data;
                }
            }

            return null;
        }

        private FrameStruct? ReadFrameData(string frameName)
        {
            FrameStruct _data = new FrameStruct();

            foreach (FrameStruct _struct in _frames)
            {
                if (_struct.name.Equals(frameName))
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
        /// Modifica il frame in uso del robot e controlla che sia stato cambiato effettivamente
        /// </summary>
        /// <param name="frameId"></param>
        /// <returns></returns>
        public int ChangeRobotFrame(int frameId)
        {
            int _checkNewFrame = -1;
            FrameStruct? _data;

            if (frameId < 0) // Frame non valido
                return 0;

            _data = ReadFrameData(frameId);
            if (_data == null) // ID Frame non trovato nella lista
                return 1;

            if (_data.Value.id == currentFrame)
                return 2;

            _robot.SetWObjCoord(frameId, _data.Value.pose);
            _robot.GetActualWObjNum(0, ref _checkNewFrame);

            if (_checkNewFrame != frameId)    // ID di risposta diverso da ID settato
                return 3;

            RobotManager.user = frameId;
            currentFrame = frameId;
            return 10;
        }

        /// <summary>
        /// Modifica il frame in uso del robot e controlla che sia stato cambiato effettivamente
        /// </summary>
        /// <param name="frameName"></param>
        /// <returns></returns>
        public int ChangeRobotFrame(string frameName)
        {
            int _checkNewFrame = -1;
            FrameStruct? _data;

            if (frameName.Length == 0 || frameName == null) // Frame non valido
                return 0;

            _data = ReadFrameData(frameName);
            if (_data == null) // ID Frame non trovato nella lista
                return 1;

            if(_data.Value.id == currentFrame)
                return 2;

            _robot.SetWObjCoord(_data.Value.id, _data.Value.pose);
            _robot.GetActualWObjNum(0, ref _checkNewFrame);

            if (_checkNewFrame != _data.Value.id)    // ID di risposta diverso da ID settato
                return 3;

            RobotManager.user = _data.Value.id;
            currentFrame = _data.Value.id;
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

            switch(errNum)
            {
                case 0:
                    errCode = "Frame ID minore di 0";
                    break;
                case 1:
                    errCode = "Frame ID o nome Frame non trovato";
                    break;
                case 2:
                    errCode = "Frame già impostato";
                    break;
                case 3:
                    errCode = "Frame impostato diverso dal frame desiderato";
                    break;
                case 10:
                    errCode = "Frame modificato correttamente";
                    break;
                default:
                    errCode = "Error number non trovato";
                    break;
            }

            return errCode;
        }
    }
}
