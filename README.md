# Sockets-In-OWA 
(or maybe QSA [Quantum Space Acquaintances], I loved that idea Rai)
### A project that aims to add socket connections to the game Outer Wilds Alpha

Fell free to look and even help with this project, the big part of handling connections is already done, and there is even some prototypes of the player movement system. 
All the sockets stuff was learned from / inspired by these series from [CaptJiggly](https://www.youtube.com/c/CaptJiggly): [multi connection server](https://www.youtube.com/watch?v=cHq2lYLA4XY) and 
[packet IO](https://www.youtube.com/watch?v=WFM0EZLE9MM&list=PLLITw-6k1t1YpH5vPPIYCKfNfLRlY_jme)

* The Server Side part works as any other [DIMOWA](https://github.com/ShoosGun/DIMOWA)'s mod, so just place it in the `mods` folder and install it
  *  Your firewall might not be triggered by it because it only allows connections made inside the computer of the host

* Totally not copying the idea from, [QSB](https://github.com/misternebula/quantum-space-buddies), why would I *ever* do that

* Current Goals:

- [ ] Tests with the `dumb client` over the internet

	- [x] In LAN (uhuuuuu LAN works!, just gotta place the IP that the debbuger gives you (it only shows the IP when the server is open to outside connections) in the Dumb Client and voilà!)
	
	- [ ] With Hamachi/Port Fowarding

~~Create a `dumb server` so that the creation of the actual client becomes easier~~ Maybe not, creating the ServerSide was already pretty hard

- [x] Create **the client**
	
	- [ ] Tests on *t**he** c**lie**nt* 

- [ ] Add a way for outside classes ('plugins') to access events (client connection/disconnection) and receive/send data [IPacketCourier]

	- [x] Add basic entity sync (with delta syncs) | only need to tested it (Shade Packet Courier)

	- [ ] Add actual syncing (with position syncs)

- [ ] Add orb sync (1- by entity sync or 2- trusting the client that it can, from some parameters, get a very simmilar orbit [I think that I'm going with the first option])

- [ ] Add quantum sync (by first knowing the only possibilities for it and then adding them in 3-4 bits, so that all of the quantum objects are in a small amount of bytes)

- [ ] Add ***FUN***

	- [ ] Beeing able to add and change nicknames from the client (with a new header) [changed reverted b'cause yes :(]
