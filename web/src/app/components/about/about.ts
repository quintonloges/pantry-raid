import { Component, inject, signal, WritableSignal } from '@angular/core';
import { HealthClient } from '../../services/pantry-raid-api';

@Component({
  selector: 'app-about',
  standalone: true,
  imports: [],
  templateUrl: './about.html',
  styleUrl: './about.scss'
})
export class AboutComponent {
  public healthStatus: WritableSignal<string> = signal<string>('Checking...');
  private healthClient: HealthClient = inject(HealthClient);

  constructor() {
    this.healthClient.get().subscribe({
      next: () => this.healthStatus.set('Online'),
      error: () => this.healthStatus.set('Backend unreachable')
    });
  }
}
