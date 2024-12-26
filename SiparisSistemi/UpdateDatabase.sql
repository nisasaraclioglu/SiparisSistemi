CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Customers` (
    `CustomerID` int NOT NULL AUTO_INCREMENT,
    `CustomerName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Budget` decimal(65,30) NOT NULL,
    `CustomerType` longtext CHARACTER SET utf8mb4 NOT NULL,
    `TotalSpent` decimal(65,30) NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Customers` PRIMARY KEY (`CustomerID`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Products` (
    `ProductID` int NOT NULL AUTO_INCREMENT,
    `ProductName` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Stock` int NOT NULL,
    `Price` decimal(65,30) NOT NULL,
    CONSTRAINT `PK_Products` PRIMARY KEY (`ProductID`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Orders` (
    `OrderID` int NOT NULL AUTO_INCREMENT,
    `CustomerID` int NOT NULL,
    `ProductID` int NOT NULL,
    `Quantity` int NOT NULL,
    `TotalPrice` decimal(65,30) NOT NULL,
    `OrderDate` datetime(6) NOT NULL,
    `Status` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Orders` PRIMARY KEY (`OrderID`),
    CONSTRAINT `FK_Orders_Customers_CustomerID` FOREIGN KEY (`CustomerID`) REFERENCES `Customers` (`CustomerID`) ON DELETE CASCADE,
    CONSTRAINT `FK_Orders_Products_ProductID` FOREIGN KEY (`ProductID`) REFERENCES `Products` (`ProductID`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Logs` (
    `LogID` int NOT NULL AUTO_INCREMENT,
    `CustomerID` int NOT NULL,
    `OrderID` int NULL,
    `LogDate` datetime(6) NOT NULL,
    `LogType` longtext CHARACTER SET utf8mb4 NOT NULL,
    `LogDetails` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Logs` PRIMARY KEY (`LogID`),
    CONSTRAINT `FK_Logs_Customers_CustomerID` FOREIGN KEY (`CustomerID`) REFERENCES `Customers` (`CustomerID`) ON DELETE CASCADE,
    CONSTRAINT `FK_Logs_Orders_OrderID` FOREIGN KEY (`OrderID`) REFERENCES `Orders` (`OrderID`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Logs_CustomerID` ON `Logs` (`CustomerID`);

CREATE INDEX `IX_Logs_OrderID` ON `Logs` (`OrderID`);

CREATE INDEX `IX_Orders_CustomerID` ON `Orders` (`CustomerID`);

CREATE INDEX `IX_Orders_ProductID` ON `Orders` (`ProductID`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241214214526_InitialCreate', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Products` ADD `ImagePath` longtext CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Products` ADD `ProductType` longtext CHARACTER SET utf8mb4 NOT NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241215220407_AddProductTypeAndImagePathToProducts', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Orders` RENAME COLUMN `Status` TO `OrderStatus`;

ALTER TABLE `Products` MODIFY COLUMN `ProductType` varchar(100) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Products` MODIFY COLUMN `ProductName` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `Products` MODIFY COLUMN `Price` decimal(10,2) NOT NULL;

ALTER TABLE `Products` MODIFY COLUMN `ImagePath` varchar(255) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Products` MODIFY COLUMN `ProductID` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `Orders` MODIFY COLUMN `TotalPrice` decimal(10,2) NOT NULL;

ALTER TABLE `Orders` MODIFY COLUMN `OrderID` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `Logs` MODIFY COLUMN `LogID` int NOT NULL AUTO_INCREMENT;

ALTER TABLE `Customers` MODIFY COLUMN `TotalSpent` decimal(65,30) NOT NULL DEFAULT 0.0;

ALTER TABLE `Customers` MODIFY COLUMN `CustomerID` int NOT NULL AUTO_INCREMENT;

CREATE TABLE `Admin` (
    `AdminID` int NOT NULL AUTO_INCREMENT,
    `Username` longtext CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Admin` PRIMARY KEY (`AdminID`)
) CHARACTER SET=utf8mb4;

INSERT INTO `Admin` (`AdminID`, `PasswordHash`, `Username`)
VALUES (1, '6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b', 'admin');

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241222114449_AddStatusToOrders', '8.0.2');

COMMIT;

START TRANSACTION;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241222114737_UpdateCustomersTable', '8.0.2');

COMMIT;

START TRANSACTION;

ALTER TABLE `Orders` DROP FOREIGN KEY `FK_Orders_Customers_CustomerID`;

ALTER TABLE `Orders` DROP FOREIGN KEY `FK_Orders_Products_ProductID`;

ALTER TABLE `Orders` MODIFY COLUMN `OrderStatus` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Pending';

ALTER TABLE `Orders` ADD CONSTRAINT `FK_Orders_Customers` FOREIGN KEY (`CustomerID`) REFERENCES `Customers` (`CustomerID`);

ALTER TABLE `Orders` ADD CONSTRAINT `FK_Orders_Products` FOREIGN KEY (`ProductID`) REFERENCES `Products` (`ProductID`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241226201726_UpdateOrdersConfiguration', '8.0.2');

COMMIT;

START TRANSACTION;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241226204127_UpdateSchema', '8.0.2');

COMMIT;

