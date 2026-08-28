using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using StackAttack.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace StackAttack.Tests
{
    public sealed class LevelBuilderTests
    {
        private Camera _previousMainCamera;
        private GameObject _cameraObject;
        private GameObject _rootObject;
        private GameObject _columnSource;
        private LevelDefinition _level;

        [SetUp]
        public void SetUp()
        {
            _previousMainCamera = Camera.main;
            if (_previousMainCamera != null)
            {
                _previousMainCamera.tag = "Untagged";
            }

            _cameraObject = new GameObject("Level Builder Test Camera", typeof(Camera));
            _cameraObject.tag = "MainCamera";
            _cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = _cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 1f;

            _columnSource = new GameObject("Column Source");
            _columnSource.transform.position = new Vector3(100f, 100f, 0f);
            var obstacleSource = new GameObject(
                "Obstacle Source",
                typeof(SpriteRenderer),
                typeof(BoxCollider2D),
                typeof(Obstacle));
            obstacleSource.transform.SetParent(_columnSource.transform, false);
            obstacleSource.transform.localScale = Vector3.one * 0.5f;
            var sourceCollider = obstacleSource.GetComponent<BoxCollider2D>();
            sourceCollider.size = new Vector2(2f, 1f);
            sourceCollider.offset = new Vector2(0.25f, 0.1f);
            var columnSource = _columnSource.AddComponent<ObstacleColumn>();

            _rootObject = new GameObject(
                "Level Root",
                typeof(ObstacleParent),
                typeof(LevelBuilder));
            var builder = _rootObject.GetComponent<LevelBuilder>();
            SetPrivateField(builder, "columnPrefab", columnSource);
            SetPrivateField(builder, "generationRoot", _rootObject.GetComponent<ObstacleParent>());

            _level = ScriptableObject.CreateInstance<LevelDefinition>();
            _level.ResizeGrid(new Vector2Int(3, 2));
            _level.SetObstacleAmount(0, 0, 1);
            _level.SetObstacleAmount(1, 0, 2);
            _level.SetCellColor(1, 0, Color.magenta);
            _level.SetObstacleAmount(2, 0, 3);
            _level.SetObstacleAmount(1, 1, 1);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_level);
            Object.DestroyImmediate(_rootObject);
            Object.DestroyImmediate(_columnSource);
            Object.DestroyImmediate(_cameraObject);

            if (_previousMainCamera != null)
            {
                _previousMainCamera.tag = "MainCamera";
            }
        }

        [UnityTest]
        public IEnumerator Build_FitsColliderGridAndPreservesColumnBehaviour()
        {
            var builder = _rootObject.GetComponent<LevelBuilder>();
            var camera = _cameraObject.GetComponent<Camera>();
            var playerStart = new Vector3(0f, -2f, 0f);

            builder.Build(_level, playerStart);
            yield return new WaitForFixedUpdate();

            Assert.That(builder.CurrentLevel, Is.SameAs(_level));
            Assert.That(builder.Columns.Count, Is.EqualTo(4));
            Assert.That(_rootObject.GetComponent<ObstacleParent>().Obstacles.Count, Is.EqualTo(7));

            var usableWidth = ViewportWorldX(camera, 0.98f) -
                              ViewportWorldX(camera, 0.02f);
            var cellWidth = usableWidth / 3f;
            var obstacleWidth = cellWidth * (1f - _level.CellSpacing);
            var halfSpacing = (cellWidth - obstacleWidth) * 0.5f;
            var firstRow = FindColumn(builder, 0, 0);
            var middleRow = FindColumn(builder, 1, 0);
            var lastRow = FindColumn(builder, 2, 0);
            var secondGridRow = FindColumn(builder, 1, 1);

            Assert.That(firstRow.Obstacles[0].transform.localScale, Is.EqualTo(Vector3.one * 0.5f));
            Assert.That(
                firstRow.transform.localScale.x,
                Is.EqualTo(obstacleWidth).Within(0.001f));
            Assert.That(firstRow.transform.localScale.y, Is.EqualTo(firstRow.transform.localScale.x).Within(0.0001f));

            Assert.That(
                firstRow.Obstacles[0].GetComponent<Collider2D>().bounds.min.x,
                Is.EqualTo(ViewportWorldX(camera, 0.02f) + halfSpacing)
                    .Within(0.01f));
            Assert.That(
                lastRow.Obstacles[0].GetComponent<Collider2D>().bounds.max.x,
                Is.EqualTo(ViewportWorldX(camera, 0.98f) - halfSpacing)
                    .Within(0.01f));

            var firstCollider =
                firstRow.Obstacles[0].GetComponent<Collider2D>();
            Assert.That(
                ColliderGap(firstCollider, middleRow),
                Is.EqualTo(cellWidth * _level.CellSpacing).Within(0.01f));

            var middleCollider = middleRow.Obstacles[0].GetComponent<Collider2D>();
            Assert.That(middleCollider.bounds.center.x, Is.EqualTo(0f).Within(0.01f));
            for (var index = 0; index < middleRow.Obstacles.Count; index++)
            {
                Assert.That(
                    middleRow.Obstacles[index].SpriteRenderer.color,
                    Is.EqualTo(Color.magenta));
            }
            Assert.That(
                middleCollider.bounds.min.y,
                Is.EqualTo(playerStart.y + 10f).Within(0.01f));
            Assert.That(
                secondGridRow.transform.position.y - middleRow.transform.position.y,
                Is.EqualTo(cellWidth).Within(0.01f));
            Assert.That(
                middleRow.transform.position.x - firstRow.transform.position.x,
                Is.EqualTo(cellWidth).Within(0.01f));

            middleRow.Obstacles[0].TakeDamage(1);
            yield return null;
            Assert.That(middleRow.RemainingCount, Is.EqualTo(1));
            Assert.That(middleRow.Amount, Is.EqualTo(2));
            Assert.That(middleRow.Obstacles[0].transform.localPosition, Is.EqualTo(Vector3.zero));
        }

        [UnityTest]
        public IEnumerator Build_UsesInitialAspectAndDoesNotReflowAfterward()
        {
            var builder = _rootObject.GetComponent<LevelBuilder>();
            var camera = _cameraObject.GetComponent<Camera>();
            camera.aspect = 0.5f;

            builder.Build(_level, Vector3.zero);
            yield return new WaitForFixedUpdate();

            var narrowScale = builder.Columns[0].transform.localScale.x;
            var builtPosition = builder.Columns[0].transform.position;
            camera.aspect = 1.5f;
            yield return new WaitForFixedUpdate();

            Assert.That(builder.Columns[0].transform.localScale.x, Is.EqualTo(narrowScale));
            Assert.That(builder.Columns[0].transform.position, Is.EqualTo(builtPosition));

            builder.Build(_level, Vector3.zero);
            yield return new WaitForFixedUpdate();
            Assert.That(builder.Columns[0].transform.localScale.x, Is.GreaterThan(narrowScale));
        }

        [UnityTest]
        public IEnumerator Build_CreatesCentroidGroupAndConfiguredOrbitBehaviour()
        {
            var groupDefinition = _level.AddGroup("Orbiters");
            _level.SetCellGroup(0, 0, groupDefinition.Id);
            _level.SetCellGroup(2, 0, groupDefinition.Id);
            var orbitConfig = new ObstacleOrbitGroupBehaviourConfig();
            SetPrivateField(orbitConfig, "degreesPerSecond", 180f);
            SetPrivateField(
                groupDefinition,
                "behaviours",
                new List<ObstacleGroupBehaviourConfig> { orbitConfig });

            var builder = _rootObject.GetComponent<LevelBuilder>();
            builder.Build(_level, Vector3.zero);

            Assert.That(builder.Groups.Count, Is.EqualTo(1));
            var runtimeGroup = builder.Groups[0];
            Assert.That(runtimeGroup.Columns.Count, Is.EqualTo(2));
            var firstColumn = FindColumn(builder, 0, 0);
            var secondColumn = FindColumn(builder, 2, 0);
            var expectedPivot =
                (firstColumn.transform.position + secondColumn.transform.position) * 0.5f;
            Assert.That(runtimeGroup.transform.position, Is.EqualTo(expectedPivot));

            var initialFirstPosition = firstColumn.transform.position;
            var initialSecondPosition = secondColumn.transform.position;
            var initialFirstRotation = firstColumn.transform.rotation;
            var initialRadius = Vector3.Distance(initialFirstPosition, expectedPivot);

            yield return new WaitForSeconds(0.1f);

            Assert.That(
                Vector3.Distance(firstColumn.transform.position, initialFirstPosition),
                Is.GreaterThan(0.01f));
            Assert.That(
                Vector3.Distance(secondColumn.transform.position, initialSecondPosition),
                Is.GreaterThan(0.01f));
            Assert.That(
                Vector3.Distance(firstColumn.transform.position, runtimeGroup.transform.position),
                Is.EqualTo(initialRadius).Within(0.01f));
            Assert.That(
                Quaternion.Angle(firstColumn.transform.rotation, initialFirstRotation),
                Is.LessThan(0.001f));
            Assert.That(
                runtimeGroup.GetComponent<ObstacleOrbitBehaviour>().DegreesPerSecond,
                Is.EqualTo(180f));
        }

        [UnityTest]
        public IEnumerator Build_CreatesCellScaledWaveThatReturnsToItsCentroid()
        {
            var groupDefinition = _level.AddGroup("Wave Orbiters");
            _level.SetCellGroup(0, 0, groupDefinition.Id);
            _level.SetCellGroup(2, 0, groupDefinition.Id);

            var waveConfig = new ObstacleWaveGroupBehaviourConfig();
            SetPrivateField(
                waveConfig,
                "direction",
                ObstacleWaveDirection.NegativeVertical);
            SetPrivateField(waveConfig, "speedInCellsPerSecond", 2f);
            SetPrivateField(waveConfig, "maximumCellDistance", 0.5f);
            var orbitConfig = new ObstacleOrbitGroupBehaviourConfig();
            SetPrivateField(orbitConfig, "degreesPerSecond", 20f);
            SetPrivateField(
                groupDefinition,
                "behaviours",
                new List<ObstacleGroupBehaviourConfig>
                {
                    waveConfig,
                    orbitConfig
                });

            var builder = _rootObject.GetComponent<LevelBuilder>();
            var camera = _cameraObject.GetComponent<Camera>();
            builder.Build(_level, Vector3.zero);

            var runtimeGroup = builder.Groups[0];
            var wave = runtimeGroup.GetComponent<ObstacleWaveBehaviour>();
            var orbit = runtimeGroup.GetComponent<ObstacleOrbitBehaviour>();
            var expectedCellSize =
                (ViewportWorldX(camera, 0.98f) -
                 ViewportWorldX(camera, 0.02f)) / 3f;
            var initialGroupPosition = runtimeGroup.transform.position;
            var firstColumn = runtimeGroup.Columns[0];
            var initialColumnRotation = firstColumn.transform.rotation;

            Assert.That(wave, Is.Not.Null);
            Assert.That(orbit, Is.Not.Null);
            Assert.That(
                wave.Direction,
                Is.EqualTo(ObstacleWaveDirection.NegativeVertical));
            Assert.That(wave.SpeedInCellsPerSecond, Is.EqualTo(2f));
            Assert.That(wave.MaximumCellDistance, Is.EqualTo(0.5f));
            Assert.That(wave.CellSize, Is.EqualTo(expectedCellSize).Within(0.001f));

            yield return new WaitForSeconds(0.2f);

            var outboundPosition = runtimeGroup.transform.position;
            Assert.That(outboundPosition.x, Is.EqualTo(initialGroupPosition.x).Within(0.001f));
            Assert.That(outboundPosition.y, Is.LessThan(initialGroupPosition.y - 0.1f));
            Assert.That(
                initialGroupPosition.y - outboundPosition.y,
                Is.LessThanOrEqualTo((0.5f * expectedCellSize) + 0.01f));

            yield return new WaitForSeconds(0.3f);

            var returnedPosition = runtimeGroup.transform.position;
            Assert.That(
                Mathf.Abs(returnedPosition.y - initialGroupPosition.y),
                Is.LessThan(
                    Mathf.Abs(outboundPosition.y - initialGroupPosition.y)));
            Assert.That(
                Quaternion.Angle(firstColumn.transform.rotation, initialColumnRotation),
                Is.LessThan(0.001f));
        }

        [Test]
        public void LevelProgressService_TracksAndClampsAgainstBuiltLevelBounds()
        {
            var builder = _rootObject.GetComponent<LevelBuilder>();
            var playerStartPosition = new Vector3(0f, -2f, 0f);
            builder.Build(_level, playerStartPosition);
            Physics2D.SyncTransforms();

            var playerObject = new GameObject("Progress Test Player");
            var service = default(LevelProgressService);
            try
            {
                var player = playerObject.AddComponent<PlayerController>();
                playerObject.transform.position = playerStartPosition;
                service = new LevelProgressService(player);
                var changeCount = 0;
                service.ProgressChanged += _ => changeCount++;

                const float completionOffset = 2.5f;
                service.Initialize(
                    playerStartPosition.y,
                    builder.Columns,
                    completionOffset);

                var expectedEndPositionY =
                    FindHighestColliderEdge(builder) + completionOffset;
                var expectedTotalDistance =
                    expectedEndPositionY - playerStartPosition.y;
                Assert.That(service.IsInitialized, Is.True);
                Assert.That(
                    service.EndPositionY,
                    Is.EqualTo(expectedEndPositionY).Within(0.001f));
                Assert.That(
                    service.TotalDistance,
                    Is.EqualTo(expectedTotalDistance).Within(0.001f));
                Assert.That(service.CurrentDistance, Is.EqualTo(0f));
                Assert.That(service.NormalizedProgress, Is.EqualTo(0f));
                Assert.That(changeCount, Is.EqualTo(1));

                playerObject.transform.position = new Vector3(
                    0f,
                    playerStartPosition.y + (expectedTotalDistance * 0.5f),
                    0f);
                service.Tick();

                Assert.That(
                    service.CurrentDistance,
                    Is.EqualTo(expectedTotalDistance * 0.5f).Within(0.001f));
                Assert.That(service.NormalizedProgress, Is.EqualTo(0.5f).Within(0.001f));

                builder.Columns[0].transform.position += Vector3.up * 100f;
                playerObject.transform.position = Vector3.up * 1000f;
                service.Tick();

                Assert.That(
                    service.TotalDistance,
                    Is.EqualTo(expectedTotalDistance).Within(0.001f));
                Assert.That(service.CurrentDistance, Is.EqualTo(expectedTotalDistance));
                Assert.That(service.NormalizedProgress, Is.EqualTo(1f));
                Assert.That(service.Progress.IsComplete, Is.True);

                playerObject.transform.position = Vector3.down * 1000f;
                service.Tick();
                Assert.That(service.CurrentDistance, Is.EqualTo(0f));
                Assert.That(service.NormalizedProgress, Is.EqualTo(0f));
            }
            finally
            {
                service?.Dispose();
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void OrbitDefaults_AreTwentyDegreesPerSecond()
        {
            var config = new ObstacleOrbitGroupBehaviourConfig();
            var runtimeObject = new GameObject("Default Orbit Behaviour");
            try
            {
                var runtimeBehaviour =
                    runtimeObject.AddComponent<ObstacleOrbitBehaviour>();

                Assert.That(config.DegreesPerSecond, Is.EqualTo(20f));
                Assert.That(runtimeBehaviour.DegreesPerSecond, Is.EqualTo(20f));
            }
            finally
            {
                Object.DestroyImmediate(runtimeObject);
            }
        }

        private static ObstacleColumn FindColumn(LevelBuilder builder, int x, int y)
        {
            var expectedName = $"Obstacle Column [{x}, {y}]";
            for (var index = 0; index < builder.Columns.Count; index++)
            {
                if (builder.Columns[index].name == expectedName)
                {
                    return builder.Columns[index];
                }
            }

            Assert.Fail($"Could not find {expectedName}.");
            return null;
        }

        private static float FindHighestColliderEdge(LevelBuilder builder)
        {
            var highestEdge = float.NegativeInfinity;
            for (var columnIndex = 0;
                 columnIndex < builder.Columns.Count;
                 columnIndex++)
            {
                var obstacles = builder.Columns[columnIndex].Obstacles;
                for (var obstacleIndex = 0;
                     obstacleIndex < obstacles.Count;
                     obstacleIndex++)
                {
                    var obstacleCollider =
                        obstacles[obstacleIndex].GetComponent<Collider2D>();
                    highestEdge = Mathf.Max(
                        highestEdge,
                        obstacleCollider.bounds.max.y);
                }
            }

            return highestEdge;
        }

        private static float ViewportWorldX(Camera camera, float viewportX)
        {
            return camera.ViewportToWorldPoint(new Vector3(viewportX, 0.5f, 10f)).x;
        }

        private static float ColliderGap(
            Collider2D leftCollider,
            ObstacleColumn rightColumn)
        {
            var rightCollider =
                rightColumn.Obstacles[0].GetComponent<Collider2D>();
            return rightCollider.bounds.min.x - leftCollider.bounds.max.x;
        }

        private static void SetPrivateField<T>(
            LevelBuilder builder,
            string fieldName,
            T value)
        {
            var field = typeof(LevelBuilder).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(builder, value);
        }

        private static void SetPrivateField<T>(
            object target,
            string fieldName,
            T value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
