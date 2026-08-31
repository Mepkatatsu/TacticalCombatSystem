using NUnit.Framework;
using Script.ClientLib;
using UnityEngine;

namespace Script.EditorLib.Test
{
    public class EntityViewTest
    {
        [Test]
        public void DirectionChangeRotatesOverMultipleUpdates()
        {
            var gameObject = new GameObject("EntityViewTest");
            try
            {
                var view = gameObject.AddComponent<EntityView>();
                // 첫 방향은 초기 회전으로 즉시 반영하고, 이후 방향 변경부터 보간하는 계약을 검증한다.
                gameObject.transform.rotation = Quaternion.LookRotation(Vector3.left);
                view.OnDirectionChanged(Vector3.forward);
                Assert.Less(
                    Quaternion.Angle(gameObject.transform.rotation, Quaternion.LookRotation(Vector3.forward)),
                    0.01f);

                view.OnDirectionChanged(Vector3.right);

                Assert.Less(Quaternion.Angle(gameObject.transform.rotation, Quaternion.LookRotation(Vector3.forward)), 0.01f);

                view.OnUpdate(50);
                var firstTickAngle = Quaternion.Angle(
                    Quaternion.LookRotation(Vector3.forward),
                    gameObject.transform.rotation);
                Assert.That(firstTickAngle, Is.EqualTo(36f).Within(0.01f));

                view.OnUpdate(50);
                Assert.That(
                    Quaternion.Angle(Quaternion.LookRotation(Vector3.forward), gameObject.transform.rotation),
                    Is.EqualTo(72f).Within(0.01f));

                view.OnUpdate(50);
                Assert.Less(Quaternion.Angle(gameObject.transform.rotation, Quaternion.LookRotation(Vector3.right)), 0.01f);

                view.OnDirectionChanged(Vector3.back);
                view.OnUpdate(50);
                Assert.That(
                    Quaternion.Angle(Quaternion.LookRotation(Vector3.right), gameObject.transform.rotation),
                    Is.EqualTo(36f).Within(0.01f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
