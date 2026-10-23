# Changelog

## [4.0.0](https://github.com/ProjectLanternDI3P1P2/reward-backend/compare/contracts-v3.0.0...contracts-v4.0.0) (2026-10-23)


### Added

* **reward:** grant enemy loot and guarantee a high-rarity item from bosses ([2103143](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/21031438756afd40ce705ac0d69c497b3d530856))

## [3.0.0](https://github.com/ProjectLanternDI3P1P2/reward-backend/compare/contracts-v2.0.0...contracts-v3.0.0) (2026-10-20)


### Added

* **contracts:** add active equipment grpc contract ([a9d9565](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/a9d956568a71479d4d9d06f7353ddb2af716fd24))
* **contracts:** define inventory grpc contract ([a32cb37](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/a32cb37bd5e1b7be09315459a663ac902990e2b8))
* **reward:** add inventory and marketplace domain ([83e43e2](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/83e43e2eda04da7b8309ad7cce0031530dba19e9))
* **reward:** generate chest loot by dungeon context ([ebd6a80](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/ebd6a80c3b164a5b7aea0efc7c2c734711ebe4a3))
* **reward:** generate chest loot by dungeon context ([c2ce115](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/c2ce115505509fcd856a6535ea1bc87156952863))


### Fixed

* edit configurations name ([9049a2e](https://github.com/ProjectLanternDI3P1P2/reward-backend/commit/9049a2ee5698b9003115d50ddb7dd5413e774c8f))

## [2.0.0](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/compare/contracts-v1.0.0...contracts-v2.0.0) (2026-09-23)


### Added

* publish player created events through RabbitMQ ([40727b6](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/40727b6125a422e1cbbeefaab2d17520c4ce2650))
* publish player created events through RabbitMQ ([cccf22e](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/cccf22eebfad251aee42150c4ba83fea8879c20c))

## 1.0.0 (2026-09-22)


### ⚠ BREAKING CHANGES

* **contracts:** release protobuf contracts independently

### Added

* **contracts:** publish protobuf package ([5df7049](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/5df7049dcb876f7f821bc58759f0fef43d7e6ec6))
* **contracts:** release protobuf contracts independently ([d7cefcb](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/d7cefcb8b517075887b9f33c90c4262bac64a212))
* Mise en place d'un exemple de service GRPC client et serveur ([550c759](https://github.com/ProjectLanternDI3P1P2/dotnet-backend-template/commit/550c759e0f5de9d7d8cebb07b2f489016918cb07))

## Changelog

All notable changes to the Protocol Buffer contracts are documented in this file.

Contract releases use the form `Vn` (`n.0.0` in the NuGet package and the
`contracts-vn.0.0` Git tag). A release is created only when files under
`Reward.Contracts/` change.
