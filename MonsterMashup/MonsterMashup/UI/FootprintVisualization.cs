using BattleTech;
using BattleTech.Rendering.UI;
using BattleTech.UI;
using IRBTModUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterMashup.UI
{
    // Lifted and modified from MPstark's AI Toolkit -
    // see https://github.com/mpstark/AIToolkit/blob/master/AIToolkit/Features/UI/InfluenceMapVisualization.cs
    // no license, but he agreed to let me use 'anything in it' in a DM
    internal class FootprintVisualization
    {
        internal GameObject TopLevelGO;

        private List<GameObject> _unusedDotPool = new List<GameObject>();
        private List<GameObject> _usedDotPool = new List<GameObject>();
        // TODO: FIx this to allow arbtirary circles
        private Mesh _circleMesh = GenerateCircleMesh(4, 20);
        private Vector3 _groundOffset = 2 * Vector3.up;

        public FootprintVisualization(string name)
        {
            TopLevelGO = new GameObject(name);
            TopLevelGO.SetActive(false);
        }

        public void Show()
        {
            TopLevelGO.SetActive(true);
        }

        public void Hide()
        {
            foreach (var dot in _usedDotPool)
                dot.SetActive(false);

            _unusedDotPool.AddRange(_usedDotPool);
            _usedDotPool.Clear();
            TopLevelGO.SetActive(false);
        }
        public void DestroyUI()
        {
            GameObject.Destroy(TopLevelGO);
        }

        public void OnActorChange(AbstractActor unit)
        {
            Hide();

            /*
             *                 
                List<Vector3> adjacentHexes = ModState.Combat.HexGrid.GetGridPointsAroundPointWithinRadius(ambushOrigin, 3); // 3 hexes should cover most large buidlings
                foreach (Vector3 adjacentHex in adjacentHexes)
                {
                    if (actorsToSpawn == 0) break;

                    Point cellPoint = new Point(ModState.Combat.MapMetaData.GetXIndex(adjacentHex.x), ModState.Combat.MapMetaData.GetZIndex(adjacentHex.z));
            */

            MapTerrainDataCell unitOrigin = unit.Combat.MapMetaData.GetCellAt(unit.CurrentPosition);
            List<Vector3> adjacentHexes = SharedState.Combat.HexGrid.GetGridPointsAroundPointWithinRadius(unit.CurrentPosition, 4);

            foreach (Vector3 hexPos in adjacentHexes)
            {
                ShowDotAt(hexPos, Color.red);
            }

            TopLevelGO.transform.position = unit.CurrentPosition;
        }

        private void ShowDotAt(Vector3 location, Color color)
        {
            GameObject dot;
            if (_unusedDotPool.Count > 0)
            {
                dot = _unusedDotPool[0];
                _unusedDotPool.RemoveAt(0);
            }
            else
            {
                dot = GenerateDot($"dot_{_unusedDotPool.Count + _usedDotPool.Count}");
            }

            _usedDotPool.Add(dot);

            Vector3 terrainHeight = SharedState.Combat.MapMetaData.GetLerpedHeightAt(location, true) * Vector3.up;
            dot.transform.position = location + _groundOffset + terrainHeight;
            dot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var renderer = dot.GetComponent<MeshRenderer>();
            renderer.material.color = color;

            dot.SetActive(true);
        }


        private GameObject GenerateDot(string name)
        {
            var dot = new GameObject(name);
            dot.transform.SetParent(TopLevelGO.transform);

            var meshFilter = dot.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = _circleMesh;

            var meshRenderer = dot.AddComponent<MeshRenderer>();
            var movementDot = CombatMovementReticle.Instance.movementDotTemplate;
            meshRenderer.material = movementDot.GetComponent<MeshRenderer>().sharedMaterial;
            meshRenderer.material.enableInstancing = false;
            meshRenderer.receiveShadows = false;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;

            var collider = dot.AddComponent<CapsuleCollider>();
            collider.center = Vector3.zero;
            collider.radius = 5f;
            collider.height = .5f;
            collider.isTrigger = true;

            dot.AddComponent<UISweep>();

            return dot;
        }

        private static Mesh GenerateCircleMesh(float radius, int numberOfPoints)
        {
            // from https://answers.unity.com/questions/944228/creating-a-smooth-round-flat-circle.html
            // not subject to license
            var angleStep = 360.0f / numberOfPoints;
            var vertexList = new List<Vector3>();
            var triangleList = new List<int>();
            var quaternion = Quaternion.Euler(0.0f, 0.0f, angleStep);

            vertexList.Add(new Vector3(0.0f, 0.0f, 0.0f));
            vertexList.Add(new Vector3(0.0f, radius, 0.0f));
            vertexList.Add(quaternion * vertexList[1]);
            triangleList.Add(0);
            triangleList.Add(1);
            triangleList.Add(2);

            for (var i = 0; i < numberOfPoints - 1; i++)
            {
                triangleList.Add(0);
                triangleList.Add(vertexList.Count - 1);
                triangleList.Add(vertexList.Count);
                vertexList.Add(quaternion * vertexList[vertexList.Count - 1]);
            }
            var mesh = new Mesh();
            mesh.vertices = vertexList.ToArray();
            mesh.triangles = triangleList.ToArray();

            return mesh;
        }
    }


}
