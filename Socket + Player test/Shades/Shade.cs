using UnityEngine;
using ServerSide.Shades.MovementConstraints;

namespace ServerSide.Shades
{
    public class Shade : MonoBehaviour
    {
        //Tem que vir a partir de um Cilindro padrão
        public ShadePacketCourier PacketCourrier
        {
            get;
            private set;
        }

        private void Start()
        {
            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            
            transform.parent = playerTransform.root;
            gameObject.layer= LayerMask.NameToLayer("Primitive");

            GetComponent<CapsuleCollider>().radius = 0.5f;
            GetComponent<CapsuleCollider>().height = 2f;
            transform.rotation = playerTransform.rotation;

            Rigidbody shadeRidigbody = gameObject.AddComponent<Rigidbody>();
            shadeRidigbody.mass = 0.001f;
            shadeRidigbody.drag = 0f;
            shadeRidigbody.angularDrag = 0f;
            shadeRidigbody.isKinematic = false;
            shadeRidigbody.constraints = RigidbodyConstraints.FreezeRotation;

            Collider taggedComponent = OWUtilities.GetTaggedComponent<Collider>(gameObject,"Player");

            if (taggedComponent.enabled)
            {
                Physics.IgnoreCollision(collider, taggedComponent);
            }
            gameObject.AddComponent<OWRigidbody>();
            GameObject shadeGODetector = new GameObject
            {
                layer = LayerMask.NameToLayer("BasicEffectVolume")
            };

            shadeGODetector.GetComponent<Transform>().parent = transform;
            shadeGODetector.GetComponent<Transform>().name = "Detector";
            shadeGODetector.AddComponent<SphereCollider>().isTrigger = true;

            shadeGODetector.AddComponent<AlignmentFieldDetector>();
            gameObject.AddComponent<AlignWithField>();

            gameObject.AddComponent<ShadeMovementModel>();
            gameObject.AddComponent<ShadeDetachHandler>();

            PacketCourrier = gameObject.AddComponent<ShadePacketCourier>();

            //Serve para fazer o player seguir o shade, "não importa o que ocorra"
            //playerTransform.GetComponent<PlayerCharacterController>().LockMovement(false);
            //playerTransform.gameObject.AddComponent<OWRigidbodyFollowsAnother>().SetConstrain(OWUtilities.GetAttachedOWRigidbody(shadeGO, false));
        }

        public void DestroyShade()
        {
            Destroy(gameObject);
        }
    }
}
