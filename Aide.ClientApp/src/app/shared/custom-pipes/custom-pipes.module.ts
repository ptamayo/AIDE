import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UtcToLocalPipe } from './utc-to-local-pipe';

@NgModule({
  declarations: [
    UtcToLocalPipe
  ],
  exports: [
    UtcToLocalPipe
  ],
  imports: [
    CommonModule
  ]
})
export class CustomPipesModule { }
