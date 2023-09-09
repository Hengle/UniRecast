using DotRecast.Detour.Crowd;

namespace DotRecast.Recast.Toolset.Tools
{
    public class CrowdOption
    {
        public int expandOptions = 1;
        public bool anticipateTurns = true;
        public bool optimizeVis = true;
        public bool optimizeTopo = true;
        public bool obstacleAvoidance = true;
        public int obstacleAvoidanceType = 3;
        public bool separation;
        public float separationWeight = 2f;

        public int GetUpdateFlags()
        {
            int updateFlags = 0;
            if (anticipateTurns)
            {
                updateFlags |= DtCrowdAgentParams.DT_CROWD_ANTICIPATE_TURNS;
            }

            if (optimizeVis)
            {
                updateFlags |= DtCrowdAgentParams.DT_CROWD_OPTIMIZE_VIS;
            }

            if (optimizeTopo)
            {
                updateFlags |= DtCrowdAgentParams.DT_CROWD_OPTIMIZE_TOPO;
            }

            if (obstacleAvoidance)
            {
                updateFlags |= DtCrowdAgentParams.DT_CROWD_OBSTACLE_AVOIDANCE;
            }

            if (separation)
            {
                updateFlags |= DtCrowdAgentParams.DT_CROWD_SEPARATION;
            }

            return updateFlags;
        }
    }
}