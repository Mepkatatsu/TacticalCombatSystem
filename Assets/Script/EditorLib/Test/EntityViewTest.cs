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
                view.OnDirectionChanged(Vector3.forward);
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
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
