import { MenuItem } from 'primeng/api';

export class MenuItemsCache {
  private stateKey = '';
  private items: MenuItem[] = [];

  get(stateParts: readonly unknown[], buildItems: () => MenuItem[]): MenuItem[] {
    const nextStateKey = JSON.stringify(stateParts);
    if (nextStateKey !== this.stateKey) {
      this.stateKey = nextStateKey;
      this.items = buildItems();
    }

    return this.items;
  }
}
