using UnityEngine;
using ClientSide.PacketCouriers.Shades.MovementConstraints;

namespace ClientSide.PacketCouriers.Shades
{
    public class Shade : MonoBehaviour
    {
        //Tem que vir a partir de um Cilindro padrão
        //public ShadeMovementModel MovementModel
        //{
        //    get;
        //    private set;
        //}

        public string Name = "";

        private void Start()
        {
            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            //center
            transform.parent = playerTransform.root;
            gameObject.layer= LayerMask.NameToLayer("Primitive");

            GetComponent<CapsuleCollider>().radius = 0.5f;
            GetComponent<CapsuleCollider>().height = 2f;

            Rigidbody shadeRidigbody = gameObject.AddComponent<Rigidbody>();
            shadeRidigbody.mass = 0.001f;
            shadeRidigbody.drag = 0f;
            shadeRidigbody.angularDrag = 0f;
            shadeRidigbody.isKinematic = false;
            //shadeRidigbody.constraints = RigidbodyConstraints.FreezeAll;

            Collider taggedComponent = OWUtilities.GetTaggedComponent<Collider>(gameObject,"Player");

            //if (taggedComponent.enabled)
            //{
            //    Physics.IgnoreCollision(collider, taggedComponent);
            //}

            taggedComponent.enabled = false; //Desabilitar colisões, ao menos se for extrapolar

            //Após testes reabilitar esses componentes mais deixalos com enable = fasle, e só liga-los se uma extrapolação for ocorrer
                     gameObject.AddComponent<OWRigidbody>().MakeKinematic();
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
