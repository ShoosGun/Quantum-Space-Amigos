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
            var solarSystemTransform = GameObject.Find("TimberHearth_Body").transform;

            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            //center
            gameObject.layer= LayerMask.NameToLayer("Primitive");

            GetComponent<CapsuleCollider>().radius = 0.5f;
            GetComponent<CapsuleCollider>().height = 2f;

            Rigidbody shadeRidigbody = gameObject.AddComponent<Rigidbody>();
            shadeRidigbody.mass = 0.001f;

            gameObject.AddComponent<OWRigidbody>().MakeKinematic();

            //if (taggedComponent.enabled)
            //{
            //    Physics.IgnoreCollision(collider, taggedComponent);
            //}

            collider.enabled = false; //Desabilitar colisões, ao menos se for extrapolar

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

            transform.parent = solarSystemTransform;
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
