//using UnityEngine;
//using ClientSide.PacketCouriers.Entities;
//using ClientSide.PacketCouriers.PersistentOWRigd;

//namespace ClientSide.PacketCouriers.Shades
//{
//    public class Shade : OWRigidbodyNetworker
//    {
//        public string Name = "";

//        public Shade GenerateShade()//GameObject.CreatePrimitive(PrimitiveType.Cylinder).AddComponent<Shade>();
//        {

//            Transform playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
//            //center
//            gameObject.layer = LayerMask.NameToLayer("Primitive");

//            GetComponent<CapsuleCollider>().radius = 0.5f;
//            GetComponent<CapsuleCollider>().height = 2f;

//            gameObject.AddComponent<Rigidbody>();
//            rigidbody.mass = 0.001f;
//            rigidbody.drag = 0f;
//            rigidbody.angularDrag = 0f;
//            rigidbody.isKinematic = true;
            
//            if (playerTransform.collider.enabled && collider.enabled)
//            {
//                Physics.IgnoreCollision(collider, playerTransform.collider);
//            }

//            gameObject.AddComponent<OWRigidbody>();
            
//            //Após testes reabilitar esses componentes mais deixalos com enable = false, e só liga-los se uma extrapolação for ocorrer
//            GameObject shadeGODetector = new GameObject
//            {
//                layer = LayerMask.NameToLayer("BasicEffectVolume")
//            };

//            shadeGODetector.GetComponent<Transform>().parent = transform;
//            shadeGODetector.GetComponent<Transform>().name = "Detector";
//            shadeGODetector.AddComponent<SphereCollider>().isTrigger = true;

//            shadeGODetector.AddComponent<AlignmentFieldDetector>();
//            gameObject.AddComponent<AlignWithField>();
            
//            gameObject.AddComponent<ShadeDetachHandler>();

//            Colliders = new Transform[] { shadeGODetector.transform, collider.transform };

//            transform.position = playerTransform.position;
//            transform.rotation = playerTransform.rotation;

//            transform.parent = GameObject.Find("TimberHearth_Body").transform;

//            //         //Serve para fazer o player seguir o shade, "não importa o que ocorra"
//            //         playerTransform.GetComponent<PlayerCharacterController>().LockMovement(false);
//            //         playerTransform.gameObject.AddComponent<OWRigidbodyFollowsAnother>().SetConstrain(OWUtilities.GetAttachedOWRigidbody(gameObject, false));

//            return this;
//        }

//        public void DestroyShade()
//        {
//            Destroy(gameObject);
//        }
//    }
//}
