import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { adminGuard, authGuard, guestGuard, playerAreaGuard } from './core/auth/auth.guard';
import { AuthService } from './core/auth/auth.service';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'feed',
    pathMatch: 'full',
  },
  {
    path: 'auth',
    loadComponent: () =>
      import('./layout/auth-layout/auth-layout.component').then(
        (m) => m.AuthLayoutComponent
      ),
    canActivate: [guestGuard],
    children: [
      { path: '', redirectTo: 'login', pathMatch: 'full' },
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/pages/login/login.component').then(
            (m) => m.LoginComponent
          ),
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/pages/register/register.component').then(
            (m) => m.RegisterComponent
          ),
      },
    ],
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/auth-layout/auth-layout.component').then(
        (m) => m.AuthLayoutComponent
      ),
    children: [
      {
        path: 'complete-profile',
        loadComponent: () =>
          import('./features/auth/pages/complete-profile/complete-profile.component').then(
            (m) => m.CompleteProfileComponent
          ),
      },
    ],
  },
  {
    path: '',
    loadComponent: () =>
      import('./layout/main-layout/main-layout.component').then(
        (m) => m.MainLayoutComponent
      ),
    canActivate: [authGuard, playerAreaGuard],
    children: [
      {
        path: 'search',
        loadComponent: () =>
          import('./features/search/pages/search/search.component').then(
            (m) => m.SearchComponent
          ),
      },
      {
        path: 'profile',
        pathMatch: 'full',
        canActivate: [() => {
          const auth = inject(AuthService);
          const router = inject(Router);
          const userId = auth.currentUser()?.userId;
          return router.createUrlTree(userId ? ['/players', userId] : ['/feed']);
        }],
        loadComponent: () =>
          import('./features/profile/pages/player-profile/player-profile.component').then(
            (m) => m.PlayerProfileComponent
          ),
      },
      {
        path: 'profile/edit',
        loadComponent: () =>
          import('./features/profile/pages/edit-profile/edit-profile.component').then(
            (m) => m.EditProfileComponent
          ),
      },
      {
        path: 'profile/stats',
        loadComponent: () =>
          import('./features/matches/pages/player-stats/player-stats.component').then(
            (m) => m.PlayerStatsComponent
          ),
      },
      {
        path: 'feed',
        loadComponent: () =>
          import('./features/feed/pages/feed/feed.component').then(
            (m) => m.FeedComponent
          ),
      },
      {
        path: 'feed/:id',
        loadComponent: () =>
          import('./features/feed/pages/post-detail/post-detail.component').then(
            (m) => m.PostDetailComponent
          ),
      },
      {
        path: 'groups',
        loadComponent: () =>
          import('./features/groups/pages/my-groups/my-groups.component').then(
            (m) => m.MyGroupsComponent
          ),
      },
      {
        path: 'groups/:groupId',
        loadComponent: () =>
          import('./features/groups/pages/group-detail/group-detail.component').then(
            (m) => m.GroupDetailComponent
          ),
      },
      {
        path: 'groups/:groupId/stats',
        loadComponent: () =>
          import('./features/groups/pages/group-stats/group-stats.component').then(
            (m) => m.GroupStatsComponent
          ),
      },
      {
        path: 'players/:userId',
        loadComponent: () =>
          import('./features/profile/pages/player-profile/player-profile.component').then(
            (m) => m.PlayerProfileComponent
          ),
      },
      {
        path: 'matches',
        loadComponent: () =>
          import('./features/matches/pages/my-matches/my-matches.component').then(
            (m) => m.MyMatchesComponent
          ),
      },
      {
        path: 'matches/create',
        loadComponent: () =>
          import('./features/matches/pages/create-match/create-match.component').then(
            (m) => m.CreateMatchComponent
          ),
      },
      {
        path: 'matches/:id',
        loadComponent: () =>
          import('./features/matches/pages/match-detail/match-detail.component').then(
            (m) => m.MatchDetailComponent
          ),
      },
      {
        path: 'chats',
        loadComponent: () =>
          import('./features/chat/pages/chats/chats.component').then(
            (m) => m.ChatsComponent
          ),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/notifications/pages/notifications/notifications.component').then(
            (m) => m.NotificationsComponent
          ),
      },
    ],
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./layout/admin-layout/admin-layout.component').then(
        (m) => m.AdminLayoutComponent
      ),
    canActivate: [authGuard, adminGuard],
    children: [
      { path: '', redirectTo: 'users', pathMatch: 'full' },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/admin/pages/admin-users/admin-users.component').then(
            (m) => m.AdminUsersComponent
          ),
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'feed',
  },
];
