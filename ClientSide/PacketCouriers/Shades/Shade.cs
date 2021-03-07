using UnityEngine;
using ClientSide.PacketCouriers.Entities;

namespace ClientSide.PacketCouriers.Shades
{
    public class Shade : NetworkedEntity
    {
        public string Name = "";

        protected void Start()//GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
        {

            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            //center
            gameObject.layer = LayerMask.NameToLayer("Primitive");

            GetComponent<CapsuleCollider>().radius = 0.5f;
            GetComponent<CapsuleCollider>().height = 2f;

            gameObject.AddComponent<Rigidbody>();
            rigidbody.mass = 0.001f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;
            rigidbody.isKinematic = true;

            //if (taggedComponent.enabled)
            //{
            //    Physics.IgnoreCollision(collider, taggedComponent);
            //}
            gameObject.AddComponent<OWRigidbody>();

            collider.enabled = false;
            //Após testes reabilitar esses componentes mais deixalos com enable = false, e só liga-los se uma extrapolação for ocorrer
            //         GameObject shadeGODetector = new GameObject
            //         {
            //             layer = LayerMask.NameToLayer("BasicEffectVolume")
            //         };

            //         shadeGODetector.GetComponent<Transform>().parent = transform;
            //         shadeGODetector.GetComponent<Transform>().name = "Detector";
            //         shadeGODetector.AddComponent<SphereCollider>().isTrigger = true;

            //         shadeGODetector.AddComponent<AlignmentFieldDetector>();
            //         gameObject.AddComponent<AlignWithField>();

            //         MovementModel = gameObject.AddComponent<ShadeMovementModel>();
            //         gameObject.AddComponent<ShadeDetachHandler>(); 

            transform.position = playerTransform.position;
            transform.rotation = playerTransform.rotation;

            transform.parent = GameObject.Find("TimberHearth_Body").transform;

            //         //Serve para fazer o player seguir o shade, "não importa o que ocorra"
            //         playerTransform.GetComponent<PlayerCharacterController>().LockMovement(false);
            //         playerTransform.gameObject.AddComponent<OWRigidbodyFollowsAnother>().SetConstrain(OWUtilities.GetAttachedOWRigidbody(gameObject, false));

        }

        public void DestroyShade()
        {
            Destroy(gameObject);
        }
    }
}
