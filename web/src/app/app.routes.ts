import { Routes } from '@angular/router';

import { HomePage } from './pages/home/home.page';
import { UploadPage } from './pages/upload/upload.page';
import { AppsPage } from './pages/apps/apps.page';
import { VersionsPage } from './pages/versions/versions.page';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: HomePage
  },
  {
    path: 'upload',
    component: UploadPage
  },
  {
    path: 'apps',
    component: AppsPage
  },
  // Backward compatibility for old links/bookmarks.
  {
    path: 'apps/:packageName',
    redirectTo: 'versions/:packageName'
  },
  {
    path: 'versions/:packageName',
    component: VersionsPage
  },
  {
    path: '**',
    redirectTo: ''
  }
];
