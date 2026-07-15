import { Injectable, signal } from '@angular/core';

export type Theme = 'dark' | 'light';
const THEME_KEY = 'cb_theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _theme = signal<Theme>(this.loadTheme());
  readonly theme = this._theme.asReadonly();

  apply() {
    document.documentElement.setAttribute('data-theme', this._theme());
  }

  set(theme: Theme) {
    this._theme.set(theme);
    localStorage.setItem(THEME_KEY, theme);
    this.apply();
  }

  toggle() {
    this.set(this._theme() === 'dark' ? 'light' : 'dark');
  }

  private loadTheme(): Theme {
    return (localStorage.getItem(THEME_KEY) as Theme) ?? 'dark';
  }
}
