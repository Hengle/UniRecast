﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DotRecast.Core;
using DotRecast.Detour.Dynamic;
using DotRecast.Detour.Dynamic.Colliders;
using DotRecast.Detour.Dynamic.Io;
using DotRecast.Recast.Toolset.Builder;
using DotRecast.Recast.Toolset.Geom;
using DotRecast.Recast.Toolset.Gizmos;

namespace DotRecast.Recast.Toolset.Tools
{
    public class RcDynamicUpdateTool : IRcToolable
    {
        private DtDynamicNavMesh dynaMesh;
        private readonly Dictionary<long, RcGizmo> colliderGizmos;

        private readonly Random random;
        private readonly DemoInputGeomProvider bridgeGeom;
        private readonly DemoInputGeomProvider houseGeom;
        private readonly DemoInputGeomProvider convexGeom;

        public RcDynamicUpdateTool(Random rand, DemoInputGeomProvider bridgeGeom, DemoInputGeomProvider houseGeom, DemoInputGeomProvider convexGeom)
        {
            this.colliderGizmos = new Dictionary<long, RcGizmo>();
            this.random = rand;
            this.bridgeGeom = bridgeGeom;
            this.houseGeom = houseGeom;
            this.convexGeom = convexGeom;
        }

        public IEnumerable<RcGizmo> GetGizmos()
        {
            return colliderGizmos.Values;
        }

        public string GetName()
        {
            return "Dynamic Updates";
        }

        public DtDynamicNavMesh GetDynamicNavMesh()
        {
            return dynaMesh;
        }

        public void RemoveShape(RcVec3f start, RcVec3f dir)
        {
            foreach (var e in colliderGizmos)
            {
                if (Hit(start, dir, e.Value.Collider.Bounds()))
                {
                    dynaMesh.RemoveCollider(e.Key);
                    colliderGizmos.Remove(e.Key);
                    break;
                }
            }
        }

        private bool Hit(RcVec3f point, RcVec3f dir, float[] bounds)
        {
            float cx = 0.5f * (bounds[0] + bounds[3]);
            float cy = 0.5f * (bounds[1] + bounds[4]);
            float cz = 0.5f * (bounds[2] + bounds[5]);
            float dx = 0.5f * (bounds[3] - bounds[0]);
            float dy = 0.5f * (bounds[4] - bounds[1]);
            float dz = 0.5f * (bounds[5] - bounds[2]);
            float rSqr = dx * dx + dy * dy + dz * dz;
            float mx = point.x - cx;
            float my = point.y - cy;
            float mz = point.z - cz;
            float c = mx * mx + my * my + mz * mz - rSqr;
            if (c <= 0.0f)
            {
                return true;
            }

            float b = mx * dir.x + my * dir.y + mz * dir.z;
            if (b > 0.0f)
            {
                return false;
            }

            float disc = b * b - c;
            return disc >= 0.0f;
        }


        public RcGizmo AddShape(RcDynamicColliderShape colliderShape, RcVec3f p)
        {
            if (dynaMesh == null)
            {
                return null;
            }

            RcGizmo colliderWithGizmo = null;
            {
                if (colliderShape == RcDynamicColliderShape.SPHERE)
                {
                    colliderWithGizmo = SphereCollider(p, dynaMesh.config.walkableClimb);
                }
                else if (colliderShape == RcDynamicColliderShape.CAPSULE)
                {
                    colliderWithGizmo = CapsuleCollider(p, dynaMesh.config.walkableClimb);
                }
                else if (colliderShape == RcDynamicColliderShape.BOX)
                {
                    colliderWithGizmo = BoxCollider(p, dynaMesh.config.walkableClimb);
                }
                else if (colliderShape == RcDynamicColliderShape.CYLINDER)
                {
                    colliderWithGizmo = CylinderCollider(p, dynaMesh.config.walkableClimb);
                }
                else if (colliderShape == RcDynamicColliderShape.COMPOSITE)
                {
                    colliderWithGizmo = CompositeCollider(p, dynaMesh.config.walkableClimb);
                }
                else if (colliderShape == RcDynamicColliderShape.TRIMESH_BRIDGE)
                {
                    colliderWithGizmo = TrimeshBridge(p, dynaMesh.config.walkableClimb);
                }
                else if (colliderShape == RcDynamicColliderShape.TRIMESH_HOUSE)
                {
                    colliderWithGizmo = TrimeshHouse(p, dynaMesh.config.walkableClimb);
                }
                else if (colliderShape == RcDynamicColliderShape.CONVEX)
                {
                    colliderWithGizmo = ConvexTrimesh(p, dynaMesh.config.walkableClimb);
                }
            }

            if (colliderWithGizmo != null)
            {
                long id = dynaMesh.AddCollider(colliderWithGizmo.Collider);
                colliderGizmos.Add(id, colliderWithGizmo);
            }

            return colliderWithGizmo;
        }

        public DtDynamicNavMesh Load(string filename, IRcCompressor compressor)
        {
            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var br = new BinaryReader(fs);
            DtVoxelFileReader reader = new DtVoxelFileReader(compressor);
            DtVoxelFile voxelFile = reader.Read(br);

            dynaMesh = new DtDynamicNavMesh(voxelFile);
            dynaMesh.config.keepIntermediateResults = true;

            colliderGizmos.Clear();

            return dynaMesh;
        }

        public void Save(string filename, bool compression, IRcCompressor compressor)
        {
            DtVoxelFile voxelFile = DtVoxelFile.From(dynaMesh);
            using var fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write);
            using var bw = new BinaryWriter(fs);
            DtVoxelFileWriter writer = new DtVoxelFileWriter(compressor);
            writer.Write(bw, voxelFile, compression);
        }


        public RcGizmo SphereCollider(RcVec3f p, float walkableClimb)
        {
            float radius = 1 + (float)random.NextDouble() * 10;
            var collider = new DtSphereCollider(p, radius, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
            var gizmo = RcGizmoFactory.Sphere(p, radius);

            return new RcGizmo(collider, gizmo);
        }

        public RcGizmo CapsuleCollider(RcVec3f p, float walkableClimb)
        {
            float radius = 0.4f + (float)random.NextDouble() * 4f;
            RcVec3f a = RcVec3f.Of(
                (1f - 2 * (float)random.NextDouble()),
                0.01f + (float)random.NextDouble(),
                (1f - 2 * (float)random.NextDouble())
            );
            a.Normalize();
            float len = 1f + (float)random.NextDouble() * 20f;
            a.x *= len;
            a.y *= len;
            a.z *= len;
            RcVec3f start = RcVec3f.Of(p.x, p.y, p.z);
            RcVec3f end = RcVec3f.Of(p.x + a.x, p.y + a.y, p.z + a.z);
            var collider = new DtCapsuleCollider(start, end, radius, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
            var gizmo = RcGizmoFactory.Capsule(start, end, radius);
            return new RcGizmo(collider, gizmo);
        }

        public RcGizmo BoxCollider(RcVec3f p, float walkableClimb)
        {
            RcVec3f extent = RcVec3f.Of(
                0.5f + (float)random.NextDouble() * 6f,
                0.5f + (float)random.NextDouble() * 6f,
                0.5f + (float)random.NextDouble() * 6f
            );
            RcVec3f forward = RcVec3f.Of((1f - 2 * (float)random.NextDouble()), 0, (1f - 2 * (float)random.NextDouble()));
            RcVec3f up = RcVec3f.Of((1f - 2 * (float)random.NextDouble()), 0.01f + (float)random.NextDouble(), (1f - 2 * (float)random.NextDouble()));
            RcVec3f[] halfEdges = Detour.Dynamic.Colliders.DtBoxCollider.GetHalfEdges(up, forward, extent);
            var collider = new DtBoxCollider(p, halfEdges, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
            var gizmo = RcGizmoFactory.Box(p, halfEdges);
            return new RcGizmo(collider, gizmo);
        }

        public RcGizmo CylinderCollider(RcVec3f p, float walkableClimb)
        {
            float radius = 0.7f + (float)random.NextDouble() * 4f;
            RcVec3f a = RcVec3f.Of(1f - 2 * (float)random.NextDouble(), 0.01f + (float)random.NextDouble(), 1f - 2 * (float)random.NextDouble());
            a.Normalize();
            float len = 2f + (float)random.NextDouble() * 20f;
            a[0] *= len;
            a[1] *= len;
            a[2] *= len;
            RcVec3f start = RcVec3f.Of(p.x, p.y, p.z);
            RcVec3f end = RcVec3f.Of(p.x + a.x, p.y + a.y, p.z + a.z);
            var collider = new DtCylinderCollider(start, end, radius, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_WATER, walkableClimb);
            var gizmo = RcGizmoFactory.Cylinder(start, end, radius);

            return new RcGizmo(collider, gizmo);
        }

        public RcGizmo CompositeCollider(RcVec3f p, float walkableClimb)
        {
            RcVec3f baseExtent = RcVec3f.Of(5, 3, 8);
            RcVec3f baseCenter = RcVec3f.Of(p.x, p.y + 3, p.z);
            RcVec3f baseUp = RcVec3f.Of(0, 1, 0);
            RcVec3f forward = RcVec3f.Of((1f - 2 * (float)random.NextDouble()), 0, (1f - 2 * (float)random.NextDouble()));
            forward.Normalize();
            RcVec3f side = RcVec3f.Cross(forward, baseUp);
            DtBoxCollider @base = new DtBoxCollider(baseCenter, Detour.Dynamic.Colliders.DtBoxCollider.GetHalfEdges(baseUp, forward, baseExtent),
                SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD, walkableClimb);
            var roofUp = RcVec3f.Zero;
            RcVec3f roofExtent = RcVec3f.Of(4.5f, 4.5f, 8f);
            var rx = RcMatrix4x4f.CreateFromRotate(45, forward.x, forward.y, forward.z);
            roofUp = MulMatrixVector(ref roofUp, rx, baseUp);
            RcVec3f roofCenter = RcVec3f.Of(p.x, p.y + 6, p.z);
            DtBoxCollider roof = new DtBoxCollider(roofCenter, Detour.Dynamic.Colliders.DtBoxCollider.GetHalfEdges(roofUp, forward, roofExtent),
                SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD, walkableClimb);
            RcVec3f trunkStart = RcVec3f.Of(
                baseCenter.x - forward.x * 15 + side.x * 6,
                p.y,
                baseCenter.z - forward.z * 15 + side.z * 6
            );
            RcVec3f trunkEnd = RcVec3f.Of(trunkStart.x, trunkStart.y + 10, trunkStart.z);
            DtCapsuleCollider trunk = new DtCapsuleCollider(trunkStart, trunkEnd, 0.5f, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD,
                walkableClimb);
            RcVec3f crownCenter = RcVec3f.Of(
                baseCenter.x - forward.x * 15 + side.x * 6, p.y + 10,
                baseCenter.z - forward.z * 15 + side.z * 6
            );
            DtSphereCollider crown = new DtSphereCollider(crownCenter, 4f, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_GRASS,
                walkableClimb);
            DtCompositeCollider collider = new DtCompositeCollider(@base, roof, trunk, crown);
            IRcGizmoMeshFilter baseGizmo = RcGizmoFactory.Box(baseCenter, Detour.Dynamic.Colliders.DtBoxCollider.GetHalfEdges(baseUp, forward, baseExtent));
            IRcGizmoMeshFilter roofGizmo = RcGizmoFactory.Box(roofCenter, Detour.Dynamic.Colliders.DtBoxCollider.GetHalfEdges(roofUp, forward, roofExtent));
            IRcGizmoMeshFilter trunkGizmo = RcGizmoFactory.Capsule(trunkStart, trunkEnd, 0.5f);
            IRcGizmoMeshFilter crownGizmo = RcGizmoFactory.Sphere(crownCenter, 4f);
            IRcGizmoMeshFilter gizmo = RcGizmoFactory.Composite(baseGizmo, roofGizmo, trunkGizmo, crownGizmo);
            return new RcGizmo(collider, gizmo);
        }

        public RcGizmo TrimeshBridge(RcVec3f p, float walkableClimb)
        {
            return TrimeshCollider(p, bridgeGeom, walkableClimb);
        }

        public RcGizmo TrimeshHouse(RcVec3f p, float walkableClimb)
        {
            return TrimeshCollider(p, houseGeom, walkableClimb);
        }

        public RcGizmo ConvexTrimesh(RcVec3f p, float walkableClimb)
        {
            float[] verts = TransformVertices(p, convexGeom, 360);
            var collider = new DtConvexTrimeshCollider(verts, convexGeom.faces,
                SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD, walkableClimb * 10);
            var gizmo = RcGizmoFactory.Trimesh(verts, convexGeom.faces);
            return new RcGizmo(collider, gizmo);
        }

        private RcGizmo TrimeshCollider(RcVec3f p, DemoInputGeomProvider geom, float walkableClimb)
        {
            float[] verts = TransformVertices(p, geom, 0);
            var collider = new DtTrimeshCollider(verts, geom.faces, SampleAreaModifications.SAMPLE_POLYAREA_TYPE_ROAD,
                walkableClimb * 10);
            var gizmo = RcGizmoFactory.Trimesh(verts, geom.faces);

            return new RcGizmo(collider, gizmo);
        }

        private float[] TransformVertices(RcVec3f p, DemoInputGeomProvider geom, float ax)
        {
            var rx = RcMatrix4x4f.CreateFromRotate((float)random.NextDouble() * ax, 1, 0, 0);
            var ry = RcMatrix4x4f.CreateFromRotate((float)random.NextDouble() * 360, 0, 1, 0);
            var m = RcMatrix4x4f.Mul(ref rx, ref ry);
            float[] verts = new float[geom.vertices.Length];
            RcVec3f v = new RcVec3f();
            RcVec3f vr = new RcVec3f();
            for (int i = 0; i < geom.vertices.Length; i += 3)
            {
                v.x = geom.vertices[i];
                v.y = geom.vertices[i + 1];
                v.z = geom.vertices[i + 2];
                MulMatrixVector(ref vr, m, v);
                vr.x += p.x;
                vr.y += p.y - 0.1f;
                vr.z += p.z;
                verts[i] = vr.x;
                verts[i + 1] = vr.y;
                verts[i + 2] = vr.z;
            }

            return verts;
        }

        private static float[] MulMatrixVector(float[] resultvector, float[] matrix, float[] pvector)
        {
            resultvector[0] = matrix[0] * pvector[0] + matrix[4] * pvector[1] + matrix[8] * pvector[2];
            resultvector[1] = matrix[1] * pvector[0] + matrix[5] * pvector[1] + matrix[9] * pvector[2];
            resultvector[2] = matrix[2] * pvector[0] + matrix[6] * pvector[1] + matrix[10] * pvector[2];
            return resultvector;
        }

        private static RcVec3f MulMatrixVector(ref RcVec3f resultvector, RcMatrix4x4f matrix, RcVec3f pvector)
        {
            resultvector.x = matrix.M11 * pvector.x + matrix.M21 * pvector.y + matrix.M31 * pvector.z;
            resultvector.y = matrix.M12 * pvector.x + matrix.M22 * pvector.y + matrix.M32 * pvector.z;
            resultvector.z = matrix.M13 * pvector.x + matrix.M23 * pvector.y + matrix.M33 * pvector.z;
            return resultvector;
        }

        public bool UpdateDynaMesh(TaskFactory executor)
        {
            if (dynaMesh == null)
            {
                return false;
            }

            bool updated = dynaMesh.Update(executor).Result;
            if (updated)
            {
                return false;
            }

            return true;
        }

        public bool Raycast(RcVec3f spos, RcVec3f epos, out float hitPos, out RcVec3f raycastHitPos)
        {
            RcVec3f sp = RcVec3f.Of(spos.x, spos.y + 1.3f, spos.z);
            RcVec3f ep = RcVec3f.Of(epos.x, epos.y + 1.3f, epos.z);

            bool hasHit = dynaMesh.VoxelQuery().Raycast(sp, ep, out hitPos);
            raycastHitPos = hasHit
                ? RcVec3f.Of(sp.x + hitPos * (ep.x - sp.x), sp.y + hitPos * (ep.y - sp.y), sp.z + hitPos * (ep.z - sp.z))
                : ep;

            return hasHit;
        }
    }
}