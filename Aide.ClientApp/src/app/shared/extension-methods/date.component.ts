declare global {  
    interface Date {  
        formatToISO8601(): string;  
    }  
   }  
   Date.prototype.formatToISO8601 = function(): string {
    return this.getFullYear().toString()
    + '-' + (this.getMonth() + 1).toString().padStart(2, '0')
    + '-' + this.getDate().toString().padStart(2, '0')
    + ' ' + this.getHours().toString().padStart(2, '0')
    + ':' + this.getMinutes().toString().padStart(2, '0')
    + ':' + this.getSeconds().toString().padStart(2, '0');
} 

export {}; 
