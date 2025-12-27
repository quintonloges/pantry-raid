import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { SearchResponseDto, SearchResultRecipeDto } from '../../services/pantry-raid-api';

@Component({
  selector: 'app-results',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatChipsModule, MatIconModule],
  templateUrl: './results.html',
  styleUrl: './results.scss'
})
export class ResultsComponent {
  @Input() results: SearchResponseDto | null = null;
  
  openRecipe(url: string | undefined): void {
    if (url) {
      window.open(url, '_blank');
    }
  }

  hasResults(): boolean {
    if (!this.results || !this.results.results) {
      return false;
    }
    return this.results.results.some(g => g.recipes && g.recipes.length > 0);
  }
}

