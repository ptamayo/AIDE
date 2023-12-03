# AIDE

## Introduction

AIDE is a comprehensive document administration management software designed specifically for service providers working with insurance companies. Its primary function is to ensure that the requisite support documentation, serving as evidence of services rendered by third-party providers, meets the standards required by insurance companies.

This software caters to a wide array of insurance services, spanning car insurance, wellness insurance for pets, home insurance, appliance insurance, mobile insurance, and more. Regardless of the service type or the insurance company involved, AIDE empowers users to set up multiple service categories, define the specific documentation necessary for each service, and seamlessly capture the customer's signature upon service completion.

One of its key features is the ability to validate and present the accepted and compliant service documentation to insurance companies, thereby demonstrating the fulfillment of services as per the required standards. This not only streamlines the documentation process but also ensures transparency and accountability in service delivery across various insurance domains.

## Architecture

AIDE comprises several microservice-oriented components that collectively form a robust solution and a cohesive system, ensuring efficient management, seamless interaction, and comprehensive functionality for insurance service providers and their associated entities.

#### Admin API

This microservice handles crucial functions such as user management, credential management, maintenance of catalogs for insurance companies, third-party stores, service types, documents, and various configurations.

#### Claims API

Responsible for overseeing order management, workflow processes, order status transitions, storage and retrieval of media files, and capturing insured customers' signatures upon service completion.

#### Notifications API

This microservice manages push notifications, facilitating the transmission of important event notifications from the backend/services to the frontend/client app. It supports different types of notifications including broadcast, individual, and group notifications.

#### Hangfire Jobs

Integrated with Azure Service Bus, this component is dedicated to executing background tasks such as generating PDF receipts, creating collages with provided images, exporting media to zip files, purging outdated orders, and more. It enables asynchronous task execution and scheduling based on specific timeframes or regular intervals.

#### API Gateway

Acting as an interface to the external world, the API Gateway selectively exposes a controlled list of endpoints required by the client app. It doesn't manage communication between microservices, as they can directly communicate with each other. The API Gateway also offers features like CORS restrictions, rate limiting, request aggregation, and load balancing.

#### Client App

A responsive Single Page Application (SPA) designed to be user-friendly across various devices, including mobile phones and tablets. It serves as the interface for users to interact with AIDE's functionalities.


## Docker and Kubernetes

AIDE has been containerized using Docker and is fully prepared for seamless deployment onto a Kubernetes cluster. This approach offers numerous advantages, including robust self-healing capabilities that automatically handle any container failures, along with the ability to dynamically scale resources as needed through auto-scaling and efficient load balancing.

Additionally, leveraging Kubernetes enables effective resource allocation and management, ensuring optimal utilization of computing resources while maintaining application performance. Moreover, it provides comprehensive monitoring and logging functionalities, offering insights into the system's health and performance for enhanced visibility and proactive issue resolution.

In summary, deploying AIDE on Kubernetes provides a resilient, scalable, and easily manageable environment, empowering the platform with features that ensure reliability, flexibility, and streamlined operations.
