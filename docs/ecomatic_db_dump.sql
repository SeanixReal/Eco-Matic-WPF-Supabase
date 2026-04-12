-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: localhost    Database: ecomatic_db
-- ------------------------------------------------------
-- Server version	8.0.45

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `customers`
--

DROP TABLE IF EXISTS `customers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `customers` (
  `rfid_tag` varchar(50) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `eco_credits` int DEFAULT '0',
  `registered_date` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`rfid_tag`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `customers`
--

LOCK TABLES `customers` WRITE;
/*!40000 ALTER TABLE `customers` DISABLE KEYS */;
/*!40000 ALTER TABLE `customers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `event_logs`
--

DROP TABLE IF EXISTS `event_logs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `event_logs` (
  `log_id` int NOT NULL AUTO_INCREMENT,
  `user_id` int DEFAULT NULL,
  `machine_id` int DEFAULT NULL,
  `event_type` varchar(50) NOT NULL,
  `description` text NOT NULL,
  `log_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`log_id`),
  KEY `user_id` (`user_id`),
  KEY `machine_id` (`machine_id`),
  CONSTRAINT `event_logs_ibfk_1` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE SET NULL,
  CONSTRAINT `event_logs_ibfk_2` FOREIGN KEY (`machine_id`) REFERENCES `vending_machines` (`machine_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=62 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `event_logs`
--

LOCK TABLES `event_logs` WRITE;
/*!40000 ALTER TABLE `event_logs` DISABLE KEYS */;
INSERT INTO `event_logs` VALUES (10,NULL,1,'PURCHASE','Item: RC Cola | Quantity: 1 | Price: ₱25.00 | Total: ₱25.00','2026-04-12 07:03:33'),(11,NULL,1,'RECYCLE','1.00 kg Glass','2026-04-12 10:15:28'),(12,NULL,1,'RECYCLE','1.00 kg Plastic','2026-04-12 10:15:32'),(13,NULL,1,'RECYCLE','20.00 kg Glass','2026-04-12 12:28:57'),(14,NULL,1,'PURCHASE','Item: Chippy | Quantity: 1 | Price: ₱32.00 | Total: ₱32.00','2026-04-12 12:56:40'),(15,NULL,1,'PURCHASE','Item: Cheese Ring | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:04:12'),(16,NULL,1,'PURCHASE','Item: Cheese Ring | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:04:12'),(17,NULL,1,'PURCHASE','Item: Cheese Ring | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:04:13'),(18,NULL,1,'PURCHASE','Item: Cheese Ring | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:04:13'),(19,NULL,1,'PURCHASE','Item: Roller Coaster | Quantity: 1 | Price: ₱28.50 | Total: ₱28.50','2026-04-12 13:04:16'),(20,NULL,1,'PURCHASE','Item: Roller Coaster | Quantity: 1 | Price: ₱28.50 | Total: ₱28.50','2026-04-12 13:04:16'),(21,NULL,1,'PURCHASE','Item: Roller Coaster | Quantity: 1 | Price: ₱28.50 | Total: ₱28.50','2026-04-12 13:04:16'),(22,NULL,1,'PURCHASE','Item: Bandaid Box | Quantity: 1 | Price: ₱20.00 | Total: ₱20.00','2026-04-12 13:04:17'),(23,NULL,1,'PURCHASE','Item: Bandaid Box | Quantity: 1 | Price: ₱20.00 | Total: ₱20.00','2026-04-12 13:04:17'),(24,NULL,1,'PURCHASE','Item: Bandaid Box | Quantity: 1 | Price: ₱20.00 | Total: ₱20.00','2026-04-12 13:04:17'),(25,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:21'),(26,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:21'),(27,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:21'),(28,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:22'),(29,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:22'),(30,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:22'),(31,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:23'),(32,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:23'),(33,NULL,1,'PURCHASE','Item: Chippy | Quantity: 1 | Price: ₱32.00 | Total: ₱32.00','2026-04-12 13:05:24'),(34,NULL,1,'PURCHASE','Item: Chippy | Quantity: 1 | Price: ₱32.00 | Total: ₱32.00','2026-04-12 13:05:25'),(35,NULL,1,'PURCHASE','Item: Chippy | Quantity: 1 | Price: ₱32.00 | Total: ₱32.00','2026-04-12 13:05:25'),(36,NULL,1,'PURCHASE','Item: Sting | Quantity: 1 | Price: ₱27.50 | Total: ₱27.50','2026-04-12 13:05:26'),(37,NULL,1,'PURCHASE','Item: Sting | Quantity: 1 | Price: ₱27.50 | Total: ₱27.50','2026-04-12 13:05:27'),(38,NULL,1,'PURCHASE','Item: Sting | Quantity: 1 | Price: ₱27.50 | Total: ₱27.50','2026-04-12 13:05:27'),(39,NULL,1,'PURCHASE','Item: RC Cola | Quantity: 1 | Price: ₱25.00 | Total: ₱25.00','2026-04-12 13:05:27'),(40,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 13:05:28'),(41,NULL,1,'PURCHASE','Item: Chippy | Quantity: 1 | Price: ₱32.00 | Total: ₱32.00','2026-04-12 13:05:29'),(42,NULL,1,'PURCHASE','Item: Pepsi | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:05:30'),(43,NULL,1,'PURCHASE','Item: Pepsi | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:05:30'),(44,NULL,1,'PURCHASE','Item: Pepsi | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:05:30'),(45,NULL,1,'PURCHASE','Item: Pepsi | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:05:30'),(46,NULL,1,'PURCHASE','Item: Pepsi | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:05:30'),(47,NULL,1,'PURCHASE','Item: Pepsi | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:05:31'),(48,NULL,1,'PURCHASE','Item: Pepsi | Quantity: 1 | Price: ₱30.00 | Total: ₱30.00','2026-04-12 13:05:31'),(49,NULL,1,'PURCHASE','Item: Coca Cola | Quantity: 1 | Price: ₱30.50 | Total: ₱30.50','2026-04-12 13:05:31'),(50,NULL,1,'PURCHASE','Item: Coca Cola | Quantity: 1 | Price: ₱30.50 | Total: ₱30.50','2026-04-12 13:05:31'),(51,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 15:14:45'),(52,NULL,1,'PURCHASE','Item: Chippy | Quantity: 1 | Price: ₱32.00 | Total: ₱32.00','2026-04-12 15:14:47'),(53,NULL,1,'PURCHASE','Item: Piattos | Quantity: 1 | Price: ₱35.00 | Total: ₱35.00','2026-04-12 15:14:48'),(54,NULL,1,'PURCHASE','Item: Eco Bag | Quantity: 1 | Price: ₱30.75 | Total: ₱30.75','2026-04-12 15:14:49'),(55,NULL,1,'PURCHASE','Item: Sting | Quantity: 1 | Price: ₱27.50 | Total: ₱27.50','2026-04-12 15:14:53'),(56,NULL,1,'PURCHASE','Item: RC Cola | Quantity: 1 | Price: ₱25.00 | Total: ₱25.00','2026-04-12 15:14:56'),(57,NULL,1,'PURCHASE','Item: Bandaid Box | Quantity: 1 | Price: ₱20.00 | Total: ₱20.00','2026-04-12 15:15:07'),(58,NULL,1,'PURCHASE','Item: Eco Bag | Quantity: 1 | Price: ₱30.75 | Total: ₱30.75','2026-04-12 15:15:07'),(59,NULL,1,'PURCHASE','Item: RC Cola | Quantity: 1 | Price: ₱25.00 | Total: ₱25.00','2026-04-12 15:38:25'),(60,NULL,1,'PURCHASE','Item: Sting | Quantity: 1 | Price: ₱27.50 | Total: ₱27.50','2026-04-12 15:38:26'),(61,NULL,1,'PURCHASE','Item: Roller Coaster | Quantity: 1 | Price: ₱28.50 | Total: ₱28.50','2026-04-12 15:38:28');
/*!40000 ALTER TABLE `event_logs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `items`
--

DROP TABLE IF EXISTS `items`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `items` (
  `item_id` int NOT NULL AUTO_INCREMENT,
  `name` varchar(100) NOT NULL,
  `type` varchar(50) NOT NULL,
  `price` decimal(10,2) NOT NULL,
  `calories` int DEFAULT '0',
  `volume_ml` int DEFAULT '0',
  `flavor_text` text,
  `image_path` varchar(255) DEFAULT NULL,
  `dispense_message` varchar(255) DEFAULT 'Enjoy your item',
  `examine_message` varchar(255) DEFAULT 'A standard vending item.',
  PRIMARY KEY (`item_id`)
) ENGINE=InnoDB AUTO_INCREMENT=14 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `items`
--

LOCK TABLES `items` WRITE;
/*!40000 ALTER TABLE `items` DISABLE KEYS */;
INSERT INTO `items` VALUES (1,'Mr Chips','Snack',30.50,160,0,NULL,'/Assets/Images/MrChips.png','Crunch away! Enjoy your Mr Chips!','A classic corn chip snack packed with cheesy, savory goodness.'),(2,'Nova','Snack',40.00,180,0,NULL,'/Assets/Images/Nova.png','Grab a multigrain bite!','A healthy, multigrain snack with a distinctive wave shape.'),(3,'Coca Cola','Drink',30.50,0,500,NULL,'/Assets/Images/CocaCola.png','Enjoy your ice-cold Coke! Stay refreshed!','The classic, iconic fizzy cola drink known worldwide.'),(4,'Pepsi','Drink',30.00,0,500,NULL,'/Assets/Images/Pepsi.png','Pop it open & enjoy the bold taste of Pepsi!','A sweet, slightly citrusy classic cola beverage.'),(5,'Bandaid Box','Misc',20.00,0,0,NULL,'/Assets/Images/BandaidBox.png','Ouch! Hope it heals quickly!','A small box of sterile adhesive bandages for minor cuts.'),(6,'Eco Bag','Misc',30.75,0,0,NULL,'/Assets/Images/EcoBag.png','Thank you for loving the Earth! Happy carrying!','A reusable, eco-friendly tote bag designed to reduce plastic waste.'),(7,'Piattos','Snack',35.00,150,0,NULL,'/Assets/Images/Piattos.png','Hexagonal crunch time! Enjoy your Piattos!','Savory hexagon-shaped potato crisps coated in delicious seasoning.'),(8,'Chippy','Snack',32.00,170,0,NULL,'/Assets/Images/Chippy.png','Time for a barbecue blast! Enjoy your Chippy!','Iconic barbecue-flavored corn chips with a hearty crunch.'),(9,'Roller Coaster','Snack',28.50,140,0,NULL,'/Assets/Images/RollerCoaster.png','Have a fun ride with Roller Coaster rings!','Fun, cheese-flavored potato rings that loop around your fingers.'),(10,'Cheese Ring','Snack',30.00,160,0,NULL,'/Assets/Images/CheeseRing.png','Cheesy goodness coming right up!','Light and airy cheese-flavored puffed corn rings.'),(11,'RC Cola','Drink',25.00,0,500,NULL,'/Assets/Images/RCCola.png','Refresh yourself with an RC Cola!','A crisp, refreshing cola with a smooth finish.'),(12,'Sting','Drink',27.50,0,500,NULL,'/Assets/Images/Sting.png','Power up! Here is your Sting energy!','A bright red, strawberry-flavored energy drink to keep you energized.');
/*!40000 ALTER TABLE `items` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `machine_inventory`
--

DROP TABLE IF EXISTS `machine_inventory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `machine_inventory` (
  `inventory_id` int NOT NULL AUTO_INCREMENT,
  `machine_id` int NOT NULL,
  `item_id` int NOT NULL,
  `stock_level` int DEFAULT '0',
  `max_capacity` int DEFAULT '15',
  PRIMARY KEY (`inventory_id`),
  UNIQUE KEY `unique_machine_item` (`machine_id`,`item_id`),
  KEY `item_id` (`item_id`),
  CONSTRAINT `machine_inventory_ibfk_1` FOREIGN KEY (`machine_id`) REFERENCES `vending_machines` (`machine_id`) ON DELETE CASCADE,
  CONSTRAINT `machine_inventory_ibfk_2` FOREIGN KEY (`item_id`) REFERENCES `items` (`item_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `machine_inventory`
--

LOCK TABLES `machine_inventory` WRITE;
/*!40000 ALTER TABLE `machine_inventory` DISABLE KEYS */;
INSERT INTO `machine_inventory` VALUES (1,1,1,14,15),(2,1,2,13,15),(3,1,3,9,15),(4,1,4,5,15),(5,1,5,10,15),(6,1,6,0,15),(7,1,7,2,15),(8,1,8,9,15),(9,1,9,1,15),(10,1,10,0,15),(11,1,11,7,15),(12,1,12,2,15),(13,6,1,14,15),(14,6,2,3,15),(15,6,3,15,15),(16,6,4,6,15),(17,6,5,1,15),(18,6,6,14,15),(19,6,7,8,15),(20,6,8,12,15),(21,6,9,8,15),(22,6,10,1,15),(23,6,11,10,15),(24,6,12,2,15);
/*!40000 ALTER TABLE `machine_inventory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `roles`
--

DROP TABLE IF EXISTS `roles`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `roles` (
  `role_id` int NOT NULL AUTO_INCREMENT,
  `role_name` varchar(50) NOT NULL,
  `description` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`role_id`),
  UNIQUE KEY `role_name` (`role_name`)
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `roles`
--

LOCK TABLES `roles` WRITE;
/*!40000 ALTER TABLE `roles` DISABLE KEYS */;
INSERT INTO `roles` VALUES (1,'Admin','Full access to all system features including sales and users.'),(2,'Inventory Manager','Access restricted to viewing and managing inventory stock for an assigned machine.');
/*!40000 ALTER TABLE `roles` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `sales_transactions`
--

DROP TABLE IF EXISTS `sales_transactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `sales_transactions` (
  `transaction_id` int NOT NULL AUTO_INCREMENT,
  `machine_id` int NOT NULL,
  `item_id` int NOT NULL,
  `amount_paid` decimal(10,2) NOT NULL,
  `transaction_date` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`transaction_id`),
  KEY `machine_id` (`machine_id`),
  KEY `item_id` (`item_id`),
  CONSTRAINT `sales_transactions_ibfk_1` FOREIGN KEY (`machine_id`) REFERENCES `vending_machines` (`machine_id`) ON DELETE CASCADE,
  CONSTRAINT `sales_transactions_ibfk_2` FOREIGN KEY (`item_id`) REFERENCES `items` (`item_id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=59 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `sales_transactions`
--

LOCK TABLES `sales_transactions` WRITE;
/*!40000 ALTER TABLE `sales_transactions` DISABLE KEYS */;
INSERT INTO `sales_transactions` VALUES (1,1,2,40.00,'2026-04-12 06:02:56'),(2,1,10,30.00,'2026-04-12 06:51:38'),(3,1,10,30.00,'2026-04-12 06:51:45'),(4,1,10,30.00,'2026-04-12 06:51:47'),(5,1,11,25.00,'2026-04-12 06:51:49'),(6,1,11,25.00,'2026-04-12 06:51:52'),(7,1,10,30.00,'2026-04-12 06:57:48'),(8,1,10,30.00,'2026-04-12 06:57:55'),(9,1,12,27.50,'2026-04-12 06:58:00'),(10,1,11,25.00,'2026-04-12 07:03:33'),(11,1,8,32.00,'2026-04-12 12:56:40'),(12,1,10,30.00,'2026-04-12 13:04:12'),(13,1,10,30.00,'2026-04-12 13:04:12'),(14,1,10,30.00,'2026-04-12 13:04:13'),(15,1,10,30.00,'2026-04-12 13:04:13'),(16,1,9,28.50,'2026-04-12 13:04:16'),(17,1,9,28.50,'2026-04-12 13:04:16'),(18,1,9,28.50,'2026-04-12 13:04:16'),(19,1,5,20.00,'2026-04-12 13:04:17'),(20,1,5,20.00,'2026-04-12 13:04:17'),(21,1,5,20.00,'2026-04-12 13:04:17'),(22,1,7,35.00,'2026-04-12 13:05:21'),(23,1,7,35.00,'2026-04-12 13:05:21'),(24,1,7,35.00,'2026-04-12 13:05:21'),(25,1,7,35.00,'2026-04-12 13:05:22'),(26,1,7,35.00,'2026-04-12 13:05:22'),(27,1,7,35.00,'2026-04-12 13:05:22'),(28,1,7,35.00,'2026-04-12 13:05:23'),(29,1,7,35.00,'2026-04-12 13:05:23'),(30,1,8,32.00,'2026-04-12 13:05:24'),(31,1,8,32.00,'2026-04-12 13:05:25'),(32,1,8,32.00,'2026-04-12 13:05:25'),(33,1,12,27.50,'2026-04-12 13:05:26'),(34,1,12,27.50,'2026-04-12 13:05:27'),(35,1,12,27.50,'2026-04-12 13:05:27'),(36,1,11,25.00,'2026-04-12 13:05:27'),(37,1,7,35.00,'2026-04-12 13:05:28'),(38,1,8,32.00,'2026-04-12 13:05:29'),(39,1,4,30.00,'2026-04-12 13:05:30'),(40,1,4,30.00,'2026-04-12 13:05:30'),(41,1,4,30.00,'2026-04-12 13:05:30'),(42,1,4,30.00,'2026-04-12 13:05:30'),(43,1,4,30.00,'2026-04-12 13:05:30'),(44,1,4,30.00,'2026-04-12 13:05:31'),(45,1,4,30.00,'2026-04-12 13:05:31'),(46,1,3,30.50,'2026-04-12 13:05:31'),(47,1,3,30.50,'2026-04-12 13:05:31'),(48,1,7,35.00,'2026-04-12 15:14:45'),(49,1,8,32.00,'2026-04-12 15:14:47'),(50,1,7,35.00,'2026-04-12 15:14:48'),(51,1,6,30.75,'2026-04-12 15:14:49'),(52,1,12,27.50,'2026-04-12 15:14:53'),(53,1,11,25.00,'2026-04-12 15:14:56'),(54,1,5,20.00,'2026-04-12 15:15:07'),(55,1,6,30.75,'2026-04-12 15:15:07'),(56,1,11,25.00,'2026-04-12 15:38:25'),(57,1,12,27.50,'2026-04-12 15:38:26'),(58,1,9,28.50,'2026-04-12 15:38:28');
/*!40000 ALTER TABLE `sales_transactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `role_id` int NOT NULL,
  `assigned_machine_id` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `username` (`username`),
  KEY `role_id` (`role_id`),
  KEY `assigned_machine_id` (`assigned_machine_id`),
  CONSTRAINT `users_ibfk_1` FOREIGN KEY (`role_id`) REFERENCES `roles` (`role_id`),
  CONSTRAINT `users_ibfk_2` FOREIGN KEY (`assigned_machine_id`) REFERENCES `vending_machines` (`machine_id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'admin','admin123',1,NULL,'2026-04-12 04:42:54'),(2,'inv_manager','manager123',2,NULL,'2026-04-12 04:56:02');
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `vending_machines`
--

DROP TABLE IF EXISTS `vending_machines`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vending_machines` (
  `machine_id` int NOT NULL AUTO_INCREMENT,
  `location_name` varchar(100) NOT NULL,
  `status` enum('Active','Maintenance','Offline') DEFAULT 'Active',
  `created_at` timestamp NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`machine_id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `vending_machines`
--

LOCK TABLES `vending_machines` WRITE;
/*!40000 ALTER TABLE `vending_machines` DISABLE KEYS */;
INSERT INTO `vending_machines` VALUES (1,'Main Hall Machine','Active','2026-04-12 05:02:17'),(6,'Library Annex','Active','2026-04-12 06:27:07');
/*!40000 ALTER TABLE `vending_machines` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-04-13  0:02:26
