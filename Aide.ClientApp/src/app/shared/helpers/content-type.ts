export function GetFileExtensionByContentType(contentType: string) {
    if (contentType === 'application/x-zip-compressed') {
        return 'zip';
    }
    return contentType.split('/')[1]; // i.e. image/jpeg
}