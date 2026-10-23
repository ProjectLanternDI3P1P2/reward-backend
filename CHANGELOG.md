# Changelog

## [1.3.0](https://github.com/ProjectLanternDI3P1P2/reward-backend/compare/v1.2.0...v1.3.0) (2026-10-23)


### Added

* **chest-loot:** expose HTTP generation and address review feedback ([a1a57e8](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/a1a57e8a12cd91fb812baf6366af122a4b7c18d9))
* **chest-loot:** expose immutable chest loot generation over HTTP ([2532c57](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/2532c57e7de56edde826e5d42f8e3301d4fa1269))
* **reward:** grant enemy loot and guarantee a high-rarity item from bosses ([2103143](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/21031438756afd40ce705ac0d69c497b3d530856))


### Fixed

* **cd:** publish images through groupe7 Harbor domain ([ef1df48](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/ef1df480abbde833280c53dbec7414fe76059c59))
* **cd:** publish through private Harbor 10.4.111.200:18443 ([17a6bce](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/17a6bce2b120a2c81a1e30012d3a7303b5ba2b87))
* **cd:** publish through private Harbor endpoint ([7f3f00d](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/7f3f00db87d062152ab796325f2e3eb1a52252f6))
* **cd:** use shared workflow with new Harbor endpoint ([764d077](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/764d077bb98b5cda34046e9267392a9617d96531))
* **chest-loot:** require all HTTP request fields ([971ba69](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/971ba693d101517d0e0227488d562cbfcd3a9233))


### Changed

* **chest-loot:** type seed difficulty and remove added docs ([a3fc833](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/a3fc833aa516f44754ef9622003aa2f9e0de2cb3))
* **reward:** apply review feedback on enemy loot ([8a64e12](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/8a64e124488cda2e2b4cce54a9a6c1afac87014d))
* **reward:** remove legacy chest identity hash ([f8f28bf](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/f8f28bfe61e6ab8be2dc11418b1a3ababa01737e))
* **reward:** use dungeon chest guids directly ([fd7e32e](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/fd7e32e6467723c5c1ffc3dad6455f70fb43748e))
* **reward:** use dungeon chest GUIDs directly ([236717d](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/236717da72c31502b749132c10dd7d3edbc34eaa))

## [1.2.0](https://github.com/ProjectLanternDI3P1P2/reward-backend/compare/v1.1.0...v1.2.0) (2026-10-20)


### Added

* **chest:** claim chest contents into the hero inventory ([5731de6](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/5731de61859d7c164ac6966257d327031c0d299e))
* **inventory:** add hero inventory endpoint ([b9fb0ef](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/b9fb0ef50518ae71e979e358f2869eda7b86bcf9))
* **inventory:** add idempotent item receipt ([a593a07](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/a593a0785d431ef319ebd05888ceea11c91ba2e5))
* **inventory:** consume consumables idempotently ([8a6226c](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/8a6226c4ada25338ceba68c23aef3076623dddab))
* **inventory:** equip eligible items ([7a5e020](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/7a5e02037d3971afcf47e15ebee8452c94d5e5eb))
* **inventory:** expose consumable consumption endpoint ([b5c9216](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/b5c921620df7266cd14fb1952d09bba503a91839))
* **inventory:** expose hero inventory over grpc ([f09e055](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/f09e055c5bef2a25c6036a44ca1571fa0130ccb8))
* **inventory:** expose item equipment endpoint ([97114d9](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/97114d947c3ceedb30deb0619e215969bd2c2f19))
* **inventory:** expose item receipt endpoint ([635e195](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/635e1954bad253beaa48fdc85c52e92dfd24372d))
* **inventory:** expose item removal endpoint ([de38ef4](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/de38ef41c78b95b8038c748742da83961e0e63ae))
* **inventory:** expose item unequipment endpoint ([e7a7c29](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/e7a7c29540a4e66a8447388a5114f6fb66ffe135))
* **inventory:** provide active equipment for combat ([c0e551c](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/c0e551c67c4113016724f2fe95e7a4e5d77d7681))
* **inventory:** report capacity limit reached ([3b4d8bb](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/3b4d8bb0a2f030bf4409f6e78ba56da20bde2710))
* **inventory:** support item removal ([dcab0c3](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/dcab0c3103d111c13d16e839ac575722e812f685))
* **inventory:** support item unequipment ([3a0f821](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/3a0f8219326c2896aa6d8eed07ea0352bce46a01))
* **persistence:** make EF mappings explicit ([4dec759](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/4dec75932a80578c6490b8ba971613e635948314))
* **reward:** add inventory and marketplace domain ([83e43e2](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/83e43e2eda04da7b8309ad7cce0031530dba19e9))
* **reward:** award unique group rewards to one eligible player ([1c056ec](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/1c056ec0137f70c29ce3213ec4edabe44705f1a9))
* **reward:** award unique group rewards to one eligible player ([77aa692](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/77aa692da8ae7e32d5ac697c6d2dfbed2ab22a8c))
* **reward:** generate chest loot by dungeon context ([ebd6a80](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/ebd6a80c3b164a5b7aea0efc7c2c734711ebe4a3))
* **reward:** generate chest loot by dungeon context ([c2ce115](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/c2ce115505509fcd856a6535ea1bc87156952863))
* **seeding:** add development inventory data ([bf6360b](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/bf6360ba9044c08f944f948b4fcb4fbf1463f89a))
* separate package and local Docker builds ([4efc8c6](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/4efc8c6d5a854f702e32f0c4dcce1695cdf6d09c))
* separate package and local Docker builds ([0a89aa0](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/0a89aa00d0b42fb125cf417b5238726ff78e3b6c))


### Fixed

* ci ([cd2a005](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/cd2a005f8655ed662ad239d48469174b5e3c7749))
* compose on Docker ([37ff38a](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/37ff38ae045dbc237727f46c8774c298b8ccd655))
* edit configurations name ([9049a2e](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/9049a2ee5698b9003115d50ddb7dd5413e774c8f))
* error on pull request ([c053a19](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/c053a198cea28d63b54ae61b1438dd140d31fa0e))
* fix error format ([e41bbdc](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/e41bbdc4b22866e6580f70eeab6a0b64c52543eb))
* fix migration ([d84dd7a](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/d84dd7a93768aedb3177c1608366ae8f8500a729))
* fix migration ([77063a4](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/77063a47ab1309dd58b4f862feed213731a5d046))
* **infrastructure:** execute commands in transactions ([84b598e](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/84b598e626480e41c8e0260ec25a7d0557419358))
* **inventory:** address equipment review findings ([470d6bf](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/470d6bf08654f85e82cddb9f0a5c4c7b2660fe1d))
* **persistence:** generate consumable use migration ([d07a186](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/d07a18616291aa547672383e5d6b06870746850f))
* **persistence:** use Guid.Empty as reward hero default ([8d70f52](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/8d70f5282d49bbaa9966bb0dcbb9d16188b982c9))
* renamme combat for reward on pipeline ([be4704f](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/be4704ff56b29b61c0a099558ccc396eccd516d3))
* **reward:** address chest loot sonar findings ([a5d3aec](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/a5d3aec2085d82e9178ac5156bf84e772f8cb385))
* **testing:** isolate integration database options ([a37386e](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/a37386eafe136a102935884e90e7123873319166))


### Changed

* **chest:** address review feedback on claim chest contents ([187ee92](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/187ee92b791d19367d6f5458194f275a75a0df83))
* **inventory:** model equipment item categories ([aa4948a](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/aa4948a1df9535717c81469928b0aa1460f5a584))
* **inventory:** model equipment slot names ([35dadc0](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/35dadc0d832fb395b22635d902fc4eba1c1f2652))
* use enums for inventory domain states ([1433f32](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/1433f322119bd090172849e75c73e664bc11f3f6))
* use enums for inventory domain states ([60d900d](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/60d900d43696293ae5f037b392330d16ecb32682))

## [1.1.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/v1.0.0...v1.1.0) (2026-09-23)


### Added

* add ef core migration tooling ([b3abca4](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/b3abca46187ee86de0eda40e2da4ef26ef68e275))
* add ef core migration tooling ([e62b3f1](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/e62b3f12e77dce1622db927db6ad55cceaf05166))
* enforce command transaction boundary ([4096387](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/40963875235dde96cbfbe0d22352baf86f1d5255))
* publish player created events through RabbitMQ ([40727b6](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/40727b6125a422e1cbbeefaab2d17520c4ce2650))
* publish player created events through RabbitMQ ([cccf22e](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/cccf22eebfad251aee42150c4ba83fea8879c20c))

## [1.0.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/v0.1.1...v1.0.0) (2026-09-22)


### ⚠ BREAKING CHANGES

* **contracts:** release protobuf contracts independently

### Added

* **contracts:** publish protobuf package ([5df7049](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/5df7049dcb876f7f821bc58759f0fef43d7e6ec6))
* **contracts:** release protobuf contracts independently ([d7cefcb](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/d7cefcb8b517075887b9f33c90c4262bac64a212))
* **grpc:** add infrastructure player client ([00a2531](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/00a2531a16d93773e7bed45485eefc608314030a))
* **grpc:** expose player service internally ([b374c80](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/b374c80622924deaa333dabc33f350175f8b1e88))
* Mise en place d'un exemple de service GRPC client et serveur ([550c759](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/550c759e0f5de9d7d8cebb07b2f489016918cb07))

## [0.1.1](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/v0.1.0...v0.1.1) (2026-09-05)


### Fixed

* run the container as the unprivileged app user ([#8](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/issues/8)) ([894be3f](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/894be3fe28e64218726b2dbc2e2bccb7efb9846b))
