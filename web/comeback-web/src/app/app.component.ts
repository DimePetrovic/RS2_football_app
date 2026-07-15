import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslationService } from './core/i18n/translation.service';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: '<router-outlet />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent implements OnInit {
  private readonly i18n = inject(TranslationService);
  private readonly theme = inject(ThemeService);

  ngOnInit() {
    this.theme.apply();
    this.i18n.load('sr-latn').subscribe();
  }
}
