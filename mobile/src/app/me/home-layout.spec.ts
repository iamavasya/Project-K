import { HOME_TILES, boardLayout, homeTiles, readStoredLayout, writeStoredLayout } from './home-layout';

describe('home layout', () => {
  it('shows every tile in the phone’s order when nothing is saved', () => {
    expect(homeTiles(null, HOME_TILES)).toEqual([...HOME_TILES]);
  });

  it('takes the dashboard board out of all the saved boards', () => {
    const layouts = [
      { boardKey: 'member-card', tileKeys: ['a'], hiddenTileKeys: ['b'] },
      { boardKey: 'dashboard', tileKeys: ['my-tasks'], hiddenTileKeys: null },
    ];
    expect(boardLayout(layouts)).toEqual({ tileKeys: ['my-tasks'], hiddenTileKeys: [] });
    expect(boardLayout([layouts[0]])).toBeNull();
    expect(boardLayout(null)).toBeNull();
  });

  it('leaves out what the person hid on the web, and keeps the rest where they were', () => {
    const layout = { tileKeys: [], hiddenTileKeys: ['my-score', 'my-tasks'] };
    expect(homeTiles(layout, ['upcoming-events', 'my-tasks', 'my-score', 'my-dues'])).toEqual(['upcoming-events', 'my-dues']);
  });

  it('follows the web’s order and slots a tile it does not know in at its own place', () => {
    const layout = {
      // The web's own tiles the phone lacks («Мій профіль», «Мої курені») are skipped.
      tileKeys: ['my-profile', 'my-dues', 'my-tasks', 'upcoming-events', 'my-kurins', 'my-tasks'],
      hiddenTileKeys: [],
    };
    expect(homeTiles(layout, ['my-duties', 'upcoming-events', 'my-tasks', 'my-dues'])).toEqual([
      'my-duties',
      'my-dues',
      'my-tasks',
      'upcoming-events',
    ]);
  });

  it('reads the layout the web keeps on this device, old and new shapes', () => {
    localStorage.setItem('tile-layout:dashboard', JSON.stringify({ tileKeys: ['my-tasks'], hiddenTileKeys: ['my-dues'] }));
    expect(readStoredLayout()).toEqual({ tileKeys: ['my-tasks'], hiddenTileKeys: ['my-dues'] });
    localStorage.setItem('tile-layout:dashboard', JSON.stringify(['my-tasks']));
    expect(readStoredLayout()).toEqual({ tileKeys: ['my-tasks'], hiddenTileKeys: [] });
    localStorage.setItem('tile-layout:dashboard', '{oops');
    expect(readStoredLayout()).toBeNull();
    writeStoredLayout({ tileKeys: ['a'], hiddenTileKeys: ['b'] });
    expect(readStoredLayout()).toEqual({ tileKeys: ['a'], hiddenTileKeys: ['b'] });
    writeStoredLayout(null);
    expect(readStoredLayout()).toBeNull();
  });
});
