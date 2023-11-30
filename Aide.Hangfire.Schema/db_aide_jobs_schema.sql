CREATE DATABASE  IF NOT EXISTS `db_aide_jobs` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `db_aide_jobs`;
-- MySQL dump 10.13  Distrib 8.0.19, for Win64 (x86_64)
--
-- Host: localhost    Database: insurancehangfiredb
-- ------------------------------------------------------
-- Server version	8.0.19

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `hangfire_aggregatedcounter`
--

DROP TABLE IF EXISTS `hangfire_aggregatedcounter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_aggregatedcounter` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Value` int NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_hangfire_CounterAggregated_Key` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=258 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_aggregatedcounter`
--

LOCK TABLES `hangfire_aggregatedcounter` WRITE;
/*!40000 ALTER TABLE `hangfire_aggregatedcounter` DISABLE KEYS */;
INSERT INTO `hangfire_aggregatedcounter` VALUES (3,'stats:succeeded',0,NULL),(29,'stats:deleted',0,NULL),(172,'stats:succeeded:2020-04-14',51,'2020-05-14 05:05:54'),(182,'stats:succeeded:2020-04-15',14,'2020-05-15 18:10:12'),(200,'stats:succeeded:2020-04-16',28,'2020-05-16 20:31:52'),(216,'stats:succeeded:2020-04-17',4,'2020-05-17 21:47:56'),(220,'stats:succeeded:2020-04-24',24,'2020-05-24 05:55:25'),(223,'stats:succeeded:2020-04-30',4,'2020-05-30 01:39:12'),(226,'stats:succeeded:2020-05-01',1,'2020-06-01 05:23:56'),(229,'stats:succeeded:2020-05-02',12,'2020-06-02 05:46:41'),(244,'stats:succeeded:2020-05-03',20,'2020-06-03 03:11:04');
/*!40000 ALTER TABLE `hangfire_aggregatedcounter` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_counter`
--

DROP TABLE IF EXISTS `hangfire_counter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_counter` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Value` int NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_hangfire_Counter_Key` (`Key`)
) ENGINE=InnoDB AUTO_INCREMENT=1018 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_counter`
--

LOCK TABLES `hangfire_counter` WRITE;
/*!40000 ALTER TABLE `hangfire_counter` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_counter` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_distributedlock`
--

DROP TABLE IF EXISTS `hangfire_distributedlock`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_distributedlock` (
  `Resource` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_distributedlock`
--

LOCK TABLES `hangfire_distributedlock` WRITE;
/*!40000 ALTER TABLE `hangfire_distributedlock` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_distributedlock` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_hash`
--

DROP TABLE IF EXISTS `hangfire_hash`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_hash` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Field` varchar(40) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Value` longtext,
  `ExpireAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_hangfire_Hash_Key_Field` (`Key`,`Field`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_hash`
--

LOCK TABLES `hangfire_hash` WRITE;
/*!40000 ALTER TABLE `hangfire_hash` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_hash` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_job`
--

DROP TABLE IF EXISTS `hangfire_job`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_job` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `StateId` int DEFAULT NULL,
  `StateName` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `InvocationData` longtext NOT NULL,
  `Arguments` longtext NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `ExpireAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_hangfire_Job_StateName` (`StateName`)
) ENGINE=InnoDB AUTO_INCREMENT=318 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_job`
--

LOCK TABLES `hangfire_job` WRITE;
/*!40000 ALTER TABLE `hangfire_job` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_job` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_jobparameter`
--

DROP TABLE IF EXISTS `hangfire_jobparameter`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_jobparameter` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `Name` varchar(40) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Value` longtext,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_hangfire_JobParameter_JobId_Name` (`JobId`,`Name`),
  KEY `FK_hangfire_JobParameter_Job` (`JobId`),
  CONSTRAINT `FK_hangfire_JobParameter_Job` FOREIGN KEY (`JobId`) REFERENCES `hangfire_job` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=667 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_jobparameter`
--

LOCK TABLES `hangfire_jobparameter` WRITE;
/*!40000 ALTER TABLE `hangfire_jobparameter` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_jobparameter` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_jobqueue`
--

DROP TABLE IF EXISTS `hangfire_jobqueue`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_jobqueue` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `FetchedAt` datetime(6) DEFAULT NULL,
  `Queue` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `FetchToken` varchar(36) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_hangfire_JobQueue_QueueAndFetchedAt` (`Queue`,`FetchedAt`)
) ENGINE=InnoDB AUTO_INCREMENT=357 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_jobqueue`
--

LOCK TABLES `hangfire_jobqueue` WRITE;
/*!40000 ALTER TABLE `hangfire_jobqueue` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_jobqueue` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_jobstate`
--

DROP TABLE IF EXISTS `hangfire_jobstate`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_jobstate` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Name` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Reason` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `Data` longtext,
  PRIMARY KEY (`Id`),
  KEY `FK_hangfire_JobState_Job` (`JobId`),
  CONSTRAINT `FK_hangfire_JobState_Job` FOREIGN KEY (`JobId`) REFERENCES `hangfire_job` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_jobstate`
--

LOCK TABLES `hangfire_jobstate` WRITE;
/*!40000 ALTER TABLE `hangfire_jobstate` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_jobstate` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_list`
--

DROP TABLE IF EXISTS `hangfire_list`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_list` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Value` longtext,
  `ExpireAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_list`
--

LOCK TABLES `hangfire_list` WRITE;
/*!40000 ALTER TABLE `hangfire_list` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_list` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_server`
--

DROP TABLE IF EXISTS `hangfire_server`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_server` (
  `Id` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Data` longtext NOT NULL,
  `LastHeartbeat` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_server`
--

LOCK TABLES `hangfire_server` WRITE;
/*!40000 ALTER TABLE `hangfire_server` DISABLE KEYS */;
INSERT INTO `hangfire_server` VALUES ('primo-pc:71224:a9a80794-26d0-43ff-b3e5-d6edb53a0382','{\"WorkerCount\":2,\"Queues\":[\"default\"],\"StartedAt\":\"2020-05-08T01:08:54.712859Z\"}','2020-05-08 03:35:34.089107');
/*!40000 ALTER TABLE `hangfire_server` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_set`
--

DROP TABLE IF EXISTS `hangfire_set`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_set` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Key` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Value` varchar(256) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL, -- If fails then change to varchar(191)
  `Score` float NOT NULL,
  `ExpireAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_hangfire_Set_Key_Value` (`Key`,`Value`)
) ENGINE=InnoDB AUTO_INCREMENT=65 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_set`
--

LOCK TABLES `hangfire_set` WRITE;
/*!40000 ALTER TABLE `hangfire_set` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_set` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `hangfire_state`
--

DROP TABLE IF EXISTS `hangfire_state`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `hangfire_state` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `JobId` int NOT NULL,
  `Name` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NOT NULL,
  `Reason` varchar(100) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  `Data` longtext,
  PRIMARY KEY (`Id`),
  KEY `FK_hangfire_HangFire_State_Job` (`JobId`),
  CONSTRAINT `FK_hangfire_HangFire_State_Job` FOREIGN KEY (`JobId`) REFERENCES `hangfire_job` (`Id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1112 DEFAULT CHARSET=utf8;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `hangfire_state`
--

LOCK TABLES `hangfire_state` WRITE;
/*!40000 ALTER TABLE `hangfire_state` DISABLE KEYS */;
/*!40000 ALTER TABLE `hangfire_state` ENABLE KEYS */;
UNLOCK TABLES;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2020-05-08 19:21:54
