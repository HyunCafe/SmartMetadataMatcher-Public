#!/usr/bin/env python3
"""Navigation and recovery guidance guards; source only, not a WPF runtime test."""
from pathlib import Path
import re
src = (Path(__file__).resolve().parents[1] / 'Source' / 'LibraryWorkspacePreview.cs').read_text(encoding='utf-8')

def body(start, end):
    assert start in src, 'Missing section: ' + start
    return src.split(start, 1)[1].split(end, 1)[0]

policy=body('private bool BusyForNavigation()', 'private void OpenClassic()')
for flag in ('scanning','checkingLiveFields','checkingBatchFields','nativeApplying'):
    assert re.search(r'\b'+flag+r'\b',policy), 'Navigation missing '+flag
classic=body('private void OpenClassic()', 'private static void AddColumn(')
assert classic.index('if (BusyForNavigation())') < classic.index('window.Close()')
assert classic.index('window.Close()') < classic.index('if (window.IsVisible) return;') < classic.index('new CatalogWindow(api, dataPath).Show();')
assert 'Switch to Classic unavailable' in classic
closing=body('window.Closing += (sender, args) => {', 'var root = new DockPanel')
assert 'if (!BusyForNavigation()) return;' in closing
assert 'args.Cancel = true;' in closing
handoff=body('var continueButton = ActionButton("Continue to Classic review"', 'var liveStatus = Label(')
assert handoff.index('if (BusyForNavigation())') < handoff.index('dialog.Close()')
assert handoff.index('window.Close()') < handoff.index('if (window.IsVisible) return;') < handoff.index('new CatalogWindow(api, dataPath, handoff)')
assert 'Advanced > Inspect recovery' in src or 'Advanced > Recovery ledgers' in src
assert 'Classic retains legacy recovery tools' in src
assert 'Advanced > Recovery ledgers ' in src and '"(read-only); use Classic' in src
print('PASS: focused navigation, Close-cancel and recovery-discovery source guards; WPF and import/undo remain unverified.')
