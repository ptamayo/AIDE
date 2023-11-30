import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { DocumentService } from 'src/app/services/document.service';
import { ProbatoryDocument } from '../../models/probatory-document';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AppError } from 'src/app/shared/common/app-error';
import { BadInput } from 'src/app/shared/common/bad-input';

@Component({
  selector: 'app-document',
  templateUrl: './document.component.html',
  styleUrls: ['./document.component.css']
})
export class DocumentComponent implements OnInit {
  action: string = "Add";
  isLoadingPage: boolean;
  saveBtnIsDisabled: boolean = false;
  readonlyFormControl: boolean = false;

  docId: number;
  doc: ProbatoryDocument | null;

  myForm: FormGroup;
  docName = new FormControl('', [Validators.required, Validators.maxLength(250)]);
  docOrientation = new FormControl('', [Validators.required]);
  docFileExtensions = new FormControl('', [Validators.required, Validators.maxLength(250)]);

  constructor(
    private route: ActivatedRoute, 
    private snackBar: MatSnackBar,
    private dataService: DocumentService) { }

  ngOnInit() {
    this.myForm = new FormGroup({
      docName: this.docName,
      docOrientation: this.docOrientation,
      docFileExtensions: this.docFileExtensions
    });

    this.docId = +this.route.snapshot.paramMap.get('id');
    if (this.docId) {
      this.action = "Edit";
    }
    this.populate();
  }

  populate() {
    if (this.docId) {
      this.isLoadingPage = true;
      this.dataService.getById(this.docId).subscribe((data: ProbatoryDocument) => {
        if (data) {
          this.doc = data;
          this.docId = this.doc.id;
          this.docName.setValue(this.doc.name);
          this.docOrientation.setValue(this.doc.orientation);
          this.docFileExtensions.setValue(this.doc.acceptedFileExtensions)
        }
        this.isLoadingPage = false;
      });
    }
  }

  toModel(): ProbatoryDocument {
    const doc: ProbatoryDocument = {
      id: this.docId,
      name: this.docName.value,
      orientation: this.docOrientation.value,
      acceptedFileExtensions: this.docFileExtensions.value
    };
    return doc;
  }

  upsert(){
    if (!this.myForm.valid) return;
    this.doc = this.toModel();
    if (!this.docId) {
      this.dataService.insert(this.doc)
        .subscribe((response: ProbatoryDocument) => {
          this.docId = response.id;
          this.openSnackBar("Operation completed.", "Dismiss");
        },
        (error: AppError) => {
          if (error instanceof BadInput) {
            this.openSnackBar(error.originalError.message, "Dismiss");
          }
          else throw error;
        })
        .add(() => {
          this.saveBtnIsDisabled = false;
        });
    } else {
      this.dataService.update(this.doc)
        .subscribe(() => {
          this.openSnackBar("Operation completed.", "Dismiss");
        },
        (error: AppError) => {
          if (error instanceof BadInput) {
            this.openSnackBar(error.originalError.message, "Dismiss");
          }
          else throw error;
        })
        .add(() => this.saveBtnIsDisabled = false);
    }
  }

  openSnackBar(message: string, action: string) {
    this.snackBar.open(message, action, {
      duration: 3000,
    });
  }
}
