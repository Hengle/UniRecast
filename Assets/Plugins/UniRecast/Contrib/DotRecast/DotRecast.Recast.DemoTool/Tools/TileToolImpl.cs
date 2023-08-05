﻿using System.Linq;
using System.Collections.Immutable;
using DotRecast.Core;
using DotRecast.Recast.DemoTool.Builder;

namespace DotRecast.Recast.DemoTool.Tools
{
    public class TileToolImpl : ISampleTool
    {
        private Sample _sample;

        public string GetName()
        {
            return "Create Tiles";
        }

        public void SetSample(Sample sample)
        {
            _sample = sample;
        }

        public Sample GetSample()
        {
            return _sample;
        }

        public void RemoveAllTiles()
        {
            var settings = _sample.GetSettings();
            var geom = _sample.GetInputGeom();
            var navMesh = _sample.GetNavMesh();

            if (null == settings || null == geom || navMesh == null)
                return;

            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();
            int gw = 0, gh = 0;
            RcUtils.CalcGridSize(bmin, bmax, settings.cellSize, out gw, out gh);

            int ts = settings.tileSize;
            int tw = (gw + ts - 1) / ts;
            int th = (gh + ts - 1) / ts;

            for (int y = 0; y < th; ++y)
            {
                for (int x = 0; x < tw; ++x)
                {
                    var tileRef = navMesh.GetTileRefAt(x, y, 0);
                    navMesh.RemoveTile(tileRef);
                }
            }
        }

        public void BuildAllTiles()
        {
            var settings = _sample.GetSettings();
            var geom = _sample.GetInputGeom();
            var navMesh = _sample.GetNavMesh();

            if (null == settings || null == geom || navMesh == null)
                return;

            var bmin = geom.GetMeshBoundsMin();
            var bmax = geom.GetMeshBoundsMax();
            int gw = 0, gh = 0;
            RcUtils.CalcGridSize(bmin, bmax, settings.cellSize, out gw, out gh);

            int ts = settings.tileSize;
            int tw = (gw + ts - 1) / ts;
            int th = (gh + ts - 1) / ts;

            for (int y = 0; y < th; ++y)
            {
                for (int x = 0; x < tw; ++x)
                {
                    BuildTile(x, y, out var tileBuildTicks, out var tileTriCount, out var tileMemUsage);
                }
            }
        }

        public bool BuildTile(int tx, int ty, out long tileBuildTicks, out int tileTriCount, out int tileMemUsage)
        {
            tileBuildTicks = 0;
            tileTriCount = 0; // ...
            tileMemUsage = 0; // ...

            var settings = _sample.GetSettings();
            var geom = _sample.GetInputGeom();
            var navMesh = _sample.GetNavMesh();

            var availableTileCount = navMesh.GetAvailableTileCount();
            if (0 >= availableTileCount)
            {
                return false;
            }

            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();

            RcConfig cfg = new RcConfig(
                true,
                settings.tileSize,
                settings.tileSize,
                RcConfig.CalcBorder(settings.agentRadius, settings.cellSize),
                RcPartitionType.OfValue(settings.partitioning),
                settings.cellSize,
                settings.cellHeight,
                settings.agentMaxSlope,
                settings.filterLowHangingObstacles,
                settings.filterLedgeSpans,
                settings.filterWalkableLowHeightSpans,
                settings.agentHeight,
                settings.agentRadius,
                settings.agentMaxClimb,
                settings.minRegionSize * settings.minRegionSize * settings.cellSize * settings.cellSize,
                settings.mergedRegionSize * settings.mergedRegionSize * settings.cellSize * settings.cellSize,
                settings.edgeMaxLen,
                settings.edgeMaxError,
                settings.vertsPerPoly,
                true,
                settings.detailSampleDist,
                settings.detailSampleMaxError,
                SampleAreaModifications.SAMPLE_AREAMOD_WALKABLE
            );

            var beginTick = RcFrequency.Ticks;
            var rb = new RecastBuilder();
            var result = rb.BuildTile(geom, cfg, bmin, bmax, tx, ty, new RcAtomicInteger(0), 1);

            var tb = new TileNavMeshBuilder();
            var meshData = tb.BuildMeshData(geom,
                settings.cellSize, settings.cellHeight, settings.agentHeight, settings.agentRadius, settings.agentMaxClimb,
                ImmutableArray.Create(result)
            ).FirstOrDefault();

            if (null == meshData)
                return false;

            navMesh.UpdateTile(meshData, 0);

            tileBuildTicks = RcFrequency.Ticks - beginTick;
            tileTriCount = 0; // ...
            tileMemUsage = 0; // ...

            return true;
        }

        public bool BuildTile(RcVec3f pos, out long tileBuildTicks, out int tileTriCount, out int tileMemUsage)
        {
            var settings = _sample.GetSettings();
            var geom = _sample.GetInputGeom();
            var navMesh = _sample.GetNavMesh();

            tileBuildTicks = 0;
            tileTriCount = 0;
            tileMemUsage = 0;

            if (null == settings || null == geom || navMesh == null)
                return false;

            float ts = settings.tileSize * settings.cellSize;

            RcVec3f bmin = geom.GetMeshBoundsMin();
            RcVec3f bmax = geom.GetMeshBoundsMax();

            int tx = (int)((pos.x - bmin[0]) / ts);
            int ty = (int)((pos.z - bmin[2]) / ts);

            return BuildTile(tx, ty, out tileBuildTicks, out tileTriCount, out tileMemUsage);
        }

        public bool RemoveTile(RcVec3f pos)
        {
            var settings = _sample.GetSettings();
            var geom = _sample.GetInputGeom();
            var navMesh = _sample.GetNavMesh();

            if (null == settings || null == geom || navMesh == null)
                return false;

            float ts = settings.tileSize * settings.cellSize;

            var bmin = geom.GetMeshBoundsMin();

            int tx = (int)((pos.x - bmin[0]) / ts);
            int ty = (int)((pos.z - bmin[2]) / ts);

            var tileRef = navMesh.GetTileRefAt(tx, ty, 0);
            navMesh.RemoveTile(tileRef);

            return true;
        }
    }
}