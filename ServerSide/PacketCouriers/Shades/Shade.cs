using UnityEngine;
using ServerSide.PacketCouriers.Entities;

namespace ServerSide.PacketCouriers.Shades
{
    public class Shade : NetworkedEntity
    {
        //Tem que vir a partir de um Cilindro padrão
        public ShadeMovementModel MovementModel
        {
            get;
            private set;
        }

        public string Name = "";

        private void Start()
        {
            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            transform.parent = playerTransform.root;
            gameObject.layer= LayerMask.NameToLayer("Primitive");

            GetComponent<CapsuleCollider>().radius = 0.5f;
            GetComponent<CapsuleCollider>().height = 2f;
            
            GetComponent<Rigidbody>().mass = 0.001f;
            GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

            Collider taggedComponent = OWUtilities.GetTaggedComponent<Collider>(gameObject,"Player");

            if (taggedComponent.enabled)
            {
                Physics.IgnoreCollision(collider, taggedComponent);
            }

            GameObject shadeGODetector = new GameObject
            {
                layer = LayerMask.NameToLayer("BasicEffectVolume")
            };

            shadeGODetector.GetComponent<Transform>().parent = transform;
            shadeGODetector.GetComponent<Transform>().name = "Detector";
            shadeGODetector.AddComponent<SphereCollider>().isTrigger = true;

            shadeGODetector.AddComponent<AlignmentFieldDetector>();
            gameObject.AddComponent<AlignWithField>();

            MovementModel = gameObject.AddComponent<ShadeMovementModel>();
            gameObject.AddComponent<ShadeDetachHandler>();
            
			transform.position = playerTransform.position;
            transform.rotation = playerTransform.rotation;
        }

        public void DestroyShade()
        {
            Destroy(gameObject);
        }
    }
}
