import { OpenMediaRequest } from "src/app/models/open-media-request";
// import { GetFileExtensionByContentType } from 'src/app/shared/helpers/content-type';

export function OpenBlob(request: OpenMediaRequest) {
    const downloadURL = window.URL.createObjectURL(request.blob);
    var link = document.createElement('a');
    // const fileExtension = GetFileExtensionByContentType(request.blob.type);
    // const fileName = `${request.name}.${fileExtension}`;
    // link.download = fileName;
    link.href = downloadURL;
    link.target = "_blank"; // If you wanna open the file in a separated tab
    link.click();
    // window.URL.revokeObjectURL(downloadURL); // This line was removed because it does NOT let the users download the files
  }