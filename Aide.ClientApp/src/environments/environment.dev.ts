import { ScreenSize } from 'src/app/enums/screen-size.enum';
import { IDictionary } from 'src/app/models/dictionary';

const baseUrl = "{{baseUrl}}";

export const environment = {
  production: true,
  screenSize: ScreenSize.SM, // need revisit and determine if want to save a global value here ...
  pageSize: 10,
  notificationsPageSize: 100,
  ClaimStatus: <IDictionary<string>> { 10: "En Proceso", 20: "Completado", 25: "Cancelado", 30: "Facturado" },
  ClaimStatusInProcess: 10,
  ClaimStatusCompleted: 20,
  ClaimStatusCancelled: 25,
  ClaimStatusInvoiced: 30,
  UserRole: ["Unknown", "Sys Admin", "Insurance", "Store Admin", "Store Operator"],
  ClaimProbatoryDocumentStatus: ["In Process", "Completed"],
  AdminDocsGroupId: 1,
  PicturesGroupId: 2,
  PicturesXItemGroupId: 3,
  TpaDocumentsGroupId: 4,
  logo: "{{logo}}",
  urlAssets: "{{urlAssets}}",
  endpointProbatoryDocumentService: `${baseUrl}/probatoryDocument`,
  endpointInsuranceProbatoryDocumentService: `${baseUrl}/insuranceProbatoryDocument`,
  endpointInsuranceCollageService: `${baseUrl}/insuranceCollage`,
  endpointStoreService: `${baseUrl}/store`,
  endpointInsuranceCompanyService: `${baseUrl}/insuranceCompany`,
  endpointMediaFileUploadService: `${baseUrl}/claimProbatoryDocument/media`,
  endpointDocumentFileUploadService: `${baseUrl}/claimDocument/document`,
  endpointClaimTypeService: `${baseUrl}/claimType`,
  endpointClaimService: `${baseUrl}/claim`,
  endpointAuthService: `${baseUrl}/user`,
  endpointUserService: `${baseUrl}/user`,
  endpointMessageBroker: `${baseUrl}/messageBroker`,
  endpointNotificationHub: `${baseUrl}/notificationHub`,
  endpointNotificationService: `${baseUrl}/notificationUser`
};
