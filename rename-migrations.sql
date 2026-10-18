-- Chạy script này TRƯỚC khi chạy Migrator sau khi pull code rename migration
-- (chỉ rename các dòng đã tồn tại; migration chưa apply thì không có dòng nào)
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014111156_AddCatalogCombosAndVoting' WHERE [MigrationId] = '20261014111156_AddWave1CatalogFeatures';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014112605_AddLoyaltyFaqAndGift' WHERE [MigrationId] = '20261014112605_Wave2LoyaltyCommerce';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014113411_SeedFaqItems' WHERE [MigrationId] = '20261014113411_SeedWave2Faq';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014114805_AddContentSecurityAndRewards' WHERE [MigrationId] = '20261014114805_Wave3KitchenCommerceSecurity';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014120000_SeedCatalogData' WHERE [MigrationId] = '20261014120000_SeedWave1Catalog';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014121119_AddBranchesLoggingAndFeatureFlags' WHERE [MigrationId] = '20261014121119_Wave4Features';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014123316_AddCorporateWalletAndShifts' WHERE [MigrationId] = '20261014123316_Wave5Features';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014130248_AddDeliverySlots' WHERE [MigrationId] = '20261014130248_Wave6aOrderingExtras';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014131446_AddOrderReviewsAndPolicies' WHERE [MigrationId] = '20261014131446_Wave6bDishRatingsPolicies';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014132450_AddOrderStatusHistory' WHERE [MigrationId] = '20261014132450_Wave6cOrderOps';
UPDATE [dbo].[__EFMigrationsHistory] SET [MigrationId] = '20261014133130_AddAntiSpamLimits' WHERE [MigrationId] = '20261014133130_Wave6dAntiSpam';
