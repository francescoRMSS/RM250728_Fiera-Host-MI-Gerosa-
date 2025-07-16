using fairino;

namespace RM.src.RM250619.Classes.FR20
{
    /// <summary>
    /// 
    /// </summary>
    public class RobotPointRecordingEventArgs
    {
        public DescPose pose {  get; set; }

        public int ID { get; set; }

        public RobotPointRecordingEventArgs(int id,DescPose pose)
        {
            this.ID = id;
            this.pose = pose;
        }
    }
}
