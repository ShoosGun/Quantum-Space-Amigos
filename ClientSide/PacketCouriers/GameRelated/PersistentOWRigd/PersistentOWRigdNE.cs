//using System;
//using System.Collections.Generic;
//using System.Text;
//using ClientSide.PacketCouriers.Entities;
//using ClientSide.PacketCouriers.Shades.MovementConstraints;
//using UnityEngine;

//namespace ClientSide.PacketCouriers.PersistentOWRigd
//{
//    public class PersistentOWRigdNE : OWRigidbodyNetworker
//    {
//    //    private Transform FakeRigidbody;
//        private Transform OriginalParent;

//        public void Start()
//        {
//            OriginalParent = transform.parent;
//            //FakeRigidbody = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;

//            //FakeRigidbody.gameObject.AddComponent<Rigidbody>().isKinematic = true;
//            //FakeRigidbody.gameObject.AddComponent<OWRigidbody>();
//            //FakeRigidbody.gameObject.collider.enabled = false;
//        }

//        public new void GoToNetworkedMode()
//        {
//            base.GoToNetworkedMode();
//            transform.parent = GameObject.Find("TimberHearth_Body").transform;
//            //FakeRigidbody.position = transform.position;
//            //FakeRigidbody.rotation = transform.rotation;

//            //transform.parent = /*GameObject.Find("TimberHearth_Body").*/FakeRigidbody;

//        }

//        public new void GoToSimulationMode(bool stayKinematic = false, bool stayWithoutCollisionsAndDetectors = false)
//        {
//            base.GoToSimulationMode(stayKinematic, stayWithoutCollisionsAndDetectors);
//            transform.parent = OriginalParent;
//        }
//        //public void SetToConstrain(OWRigidbody oWRigidbody)
//        //{
//        //    ConstraintManager = gameObject.AddComponent<OWRigidbodyFollowsAnother>();
//        //    OWRigidbodyToFake = oWRigidbody;
//        //    OriginalParent = oWRigidbody.transform.parent;
//        //}

//        //public void Constrain()
//        //{
//        //    OWRigidbodyToFake.transform.parent = transform.parent;
//        //    ConstraintManager.SetConstrain(OWRigidbodyToFake, true);
//        //}

//        //public void ResetConstraint()
//        //{
//        //    OWRigidbodyToFake.transform.parent = OriginalParent;
//        //    ConstraintManager.Reset();
//        //}
//    }
//}
