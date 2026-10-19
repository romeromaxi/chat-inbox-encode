import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/inbox/presentation/pages/inbox-page/inbox-page.component').then(
        ({ InboxPageComponent }) => InboxPageComponent,
      ),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
