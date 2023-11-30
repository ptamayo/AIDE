import { Component, OnInit, Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
  selector: 'app-message-dialog',
  templateUrl: './message-dialog.component.html',
  styleUrls: ['./message-dialog.component.css']
})
export class MessageDialogComponent implements OnInit {
  title: string;
  message: string;
  icon: string; // info, warning, error
  
  constructor(@Inject(MAT_DIALOG_DATA) data) { 
    this.title = data.title;
    this.message = data.message;
    this.icon = data.icon;
  }

  ngOnInit() {
  }

}
