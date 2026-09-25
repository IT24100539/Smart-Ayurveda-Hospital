import React, { useState, useEffect } from 'react';
import { 
  getTreatments, 
  getTreatmentDetails, 
  createTreatment, 
  deactivateTreatment, 
  createScheduleEntry, 
  deleteScheduleEntry,
  TreatmentCategory
} from '../../api/treatments';
import type {
  TreatmentSummaryDto,
  TreatmentDetailDto
} from '../../api/treatments';
import { Badge, Button, Card, EmptyState, LoadingState, PageHeader } from '../ui';

// --- Shared UI SVG Icons ---
const IconPlus = () => <svg width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M12 5v14M5 12h14"/></svg>;
const IconSearch = () => <svg width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/></svg>;
const IconClose = () => <svg width="20" height="20" fill="none" stroke="currentColor" strokeWidth="2" viewBox="0 0 24 24"><path d="M6 18L18 6M6 6l12 12"/></svg>;

const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

export function TreatmentsView() {
  const [treatments, setTreatments] = useState<TreatmentSummaryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [expandedRow, setExpandedRow] = useState<string | null>(null);
  const [query, setQuery] = useState('');

  const fetchTreatments = async () => {
    setLoading(true);
    try {
      const data = await getTreatments({ page: 1, pageSize: 50 });
      setTreatments(data.items || []);
    } catch (e: any) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchTreatments();
  }, []);

  const handleDeactivate = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (confirm('Are you sure you want to deactivate this treatment?')) {
      await deactivateTreatment(id);
      fetchTreatments();
    }
  };

  const needle = query.trim().toLowerCase();
  const visible = needle
    ? treatments.filter(
        (item) =>
          item.name.toLowerCase().includes(needle) ||
          item.nameSinhala.toLowerCase().includes(needle)
      )
    : treatments;

  return (
    <div className="mx-auto max-w-7xl">
      <PageHeader
        kicker="Panchakarma catalogue"
        title="Treatments"
        description="Therapies, duration, and the days each treatment is offered."
        action={
          <Button onClick={() => setDrawerOpen(true)} className="w-full !bg-white !text-primary-dark hover:!bg-primary-muted sm:w-auto">
            <IconPlus /> New Treatment
          </Button>
        }
      />

      <img
        src="/images/herbal-oils.png"
        alt="Brass bowl of herbal oil with tulsi and neem"
        className="mb-6 h-44 w-full rounded-2xl object-cover shadow-md"
      />
      <Card className="overflow-hidden shadow-sm">
        <div className="flex gap-4 border-b border-surface-border bg-neutral-50 p-4">
          <div className="relative flex-1">
            <div className="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3 text-muted">
              <IconSearch />
            </div>
            <input
              type="text"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search treatments..."
              className="min-h-11 w-full rounded-md border border-surface-border bg-surface-raised py-2 pl-10 pr-4 text-ink outline-none ring-primary focus:ring-2"
            />
          </div>
        </div>

        {loading ? (
          <div className="p-4">
            <LoadingState label="Loading treatments…" />
          </div>
        ) : visible.length === 0 ? (
          <div className="p-4">
            <EmptyState
              title={treatments.length === 0 ? "No treatments found." : "No treatments match that search."}
              description={treatments.length === 0 ? "Add a treatment to build the panchakarma schedule." : "Try another therapy name."}
            />
          </div>
        ) : (
          <div className="overflow-x-auto">
          <table className="w-full min-w-[40rem] border-collapse text-left">
            <thead>
              <tr className="bg-neutral-50 text-sm text-muted">
                <th className="border-b border-surface-border px-4 py-3 font-semibold">Name</th>
                <th className="border-b border-surface-border px-4 py-3 font-semibold">Category</th>
                <th className="border-b border-surface-border px-4 py-3 font-semibold">Status</th>
                <th className="border-b border-surface-border px-4 py-3 text-center font-semibold">Schedule (S M T W T F S)</th>
                <th className="border-b border-surface-border px-4 py-3 text-right font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody>
              {visible.map(t => (
                <React.Fragment key={t.id}>
                  <tr 
                    className="cursor-pointer border-b border-surface-border transition hover:bg-neutral-50"
                    onClick={() => setExpandedRow(expandedRow === t.id ? null : t.id)}
                  >
                    <td className="px-4 py-4">
                      <div className="font-medium text-ink">{t.name}</div>
                      <div className="text-xs text-muted">{t.nameSinhala}</div>
                    </td>
                    <td className="px-4 py-4 text-muted">{TreatmentCategory[t.category as unknown as keyof typeof TreatmentCategory]}</td>
                    <td className="px-4 py-4">
                      <Badge tone={t.isActive ? "success" : "rejected"}>{t.isActive ? "Active" : "Inactive"}</Badge>
                    </td>
                    <td className="px-4 py-4">
                      <div className="flex justify-center gap-1">
                        {days.map((_, i) => (
                          <div
                            key={i}
                            className={`flex h-6 w-6 items-center justify-center rounded text-xs ${t.availableDays?.includes(i) ? "bg-primary-muted font-bold text-primary-dark" : "text-neutral-300"}`}
                          >
                            {t.availableDays?.includes(i) ? "✓" : "—"}
                          </div>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-4 text-right">
                      {t.isActive && (
                        <Button variant="danger" onClick={(e) => handleDeactivate(t.id, e)}>
                          Deactivate
                        </Button>
                      )}
                    </td>
                  </tr>
                  {expandedRow === t.id && (
                    <tr>
                      <td colSpan={5} className="bg-primary-muted/40 p-0">
                        <InlineScheduleEditor 
                          treatmentId={t.id} 
                          onSave={() => { setExpandedRow(null); fetchTreatments(); }}
                          onCancel={() => setExpandedRow(null)}
                        />
                      </td>
                    </tr>
                  )}
                </React.Fragment>
              ))}
            </tbody>
          </table>
          </div>
        )}
      </Card>

      {drawerOpen && (
        <div className="fixed inset-0 z-50 flex justify-end">
          <div className="absolute inset-0 bg-ink/20" onClick={() => setDrawerOpen(false)} />
          <div className="relative flex w-full max-w-md flex-col bg-surface-raised shadow-lg">
            <div className="flex items-center justify-between border-b border-surface-border p-6">
              <h2 className="font-display text-xl font-semibold text-ink">New Treatment</h2>
              <Button variant="secondary" className="min-w-11 px-3" aria-label="Close" onClick={() => setDrawerOpen(false)}>
                <IconClose />
              </Button>
            </div>
            <div className="p-6 overflow-y-auto flex-1">
              <TreatmentForm 
                onSuccess={() => { setDrawerOpen(false); fetchTreatments(); }} 
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function TreatmentForm({ onSuccess }: { onSuccess: () => void }) {
  const [formData, setFormData] = useState({
    name: '', nameSinhala: '', description: '', descriptionSinhala: '', category: 0 as TreatmentCategory, durationMinutes: 30, unitPrice: 0
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await createTreatment(formData);
    onSuccess();
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="mb-1 block text-sm font-medium text-ink">Name</label>
        <input required type="text" className="min-h-11 w-full rounded-md border border-surface-border p-2" value={formData.name} onChange={e => setFormData({...formData, name: e.target.value})} />
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium text-ink">Name (Sinhala)</label>
        <input required type="text" className="min-h-11 w-full rounded-md border border-surface-border p-2" value={formData.nameSinhala} onChange={e => setFormData({...formData, nameSinhala: e.target.value})} />
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium text-ink">Category</label>
        <select className="min-h-11 w-full rounded-md border border-surface-border p-2" value={formData.category} onChange={e => setFormData({...formData, category: Number(e.target.value) as TreatmentCategory})}>
          {Object.entries(TreatmentCategory).filter(([k]) => isNaN(Number(k))).map(([k, v]) => (
            <option key={String(v)} value={Number(v)}>{k}</option>
          ))}
        </select>
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium text-ink">Description</label>
        <textarea className="w-full rounded-md border border-surface-border p-2" value={formData.description} onChange={e => setFormData({...formData, description: e.target.value})} />
      </div>
      <div>
        <label className="mb-1 block text-sm font-medium text-ink">Description (Sinhala)</label>
        <textarea className="w-full rounded-md border border-surface-border p-2" value={formData.descriptionSinhala} onChange={e => setFormData({...formData, descriptionSinhala: e.target.value})} />
      </div>
      <div className="pt-4">
        <Button type="submit" className="w-full">Create Treatment</Button>
      </div>
    </form>
  );
}

function InlineScheduleEditor({ treatmentId, onSave, onCancel }: { treatmentId: string, onSave: () => void, onCancel: () => void }) {
  const [detail, setDetail] = useState<TreatmentDetailDto | null>(null);
  const [editedDays, setEditedDays] = useState<Record<number, { enabled: boolean, startTime: string, endTime: string, maxSlots: number, existingId?: string }>>({});
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getTreatmentDetails(treatmentId).then(data => {
      setDetail(data);
      const initial: typeof editedDays = {};
      for (let i = 0; i < 7; i++) {
        const entry = data.schedule?.find(s => s.dayOfWeek === i);
        initial[i] = {
          enabled: !!entry,
          existingId: entry?.id,
          startTime: entry?.startTime || '09:00:00',
          endTime: entry?.endTime || '17:00:00',
          maxSlots: entry?.maxSlotsPerDay || 10
        };
      }
      setEditedDays(initial);
    });
  }, [treatmentId]);

  if (!detail) return <div className="p-4"><LoadingState label="Loading schedule…" /></div>;

  const handleSave = async () => {
    setSaving(true);
    try {
      for (let i = 0; i < 7; i++) {
        const state = editedDays[i];
        const wasEnabled = !!state.existingId;
        
        if (state.enabled && !wasEnabled) {
          await createScheduleEntry(treatmentId, {
            dayOfWeek: i,
            startTime: state.startTime,
            endTime: state.endTime,
            maxSlotsPerDay: state.maxSlots
          });
        } else if (!state.enabled && wasEnabled && state.existingId) {
          await deleteScheduleEntry(treatmentId, state.existingId);
        }
      }
      onSave();
    } catch (e: any) {
      console.error(e);
      alert('Failed to save schedule');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="border-y border-primary-muted bg-surface-raised p-4 sm:p-6" onClick={e => e.stopPropagation()}>
      <div className="mb-4 flex items-center justify-between">
        <h3 className="font-display font-semibold text-ink">Edit Schedule for {detail.name}</h3>
      </div>
      <div className="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-7">
        {days.map((dayName, i) => {
          const state = editedDays[i];
          if (!state) return null;
          
          return (
            <div key={i} className={`rounded-xl border p-3 transition-colors ${state.enabled ? "border-primary bg-primary-muted" : "border-surface-border bg-neutral-50 opacity-70"}`}>
              <div className="mb-3 flex min-h-11 items-center gap-2">
                <input
                  type="checkbox"
                  checked={state.enabled}
                  onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, enabled: e.target.checked}}))}
                  className="h-5 w-5 rounded text-primary"
                />
                <span className="font-semibold text-sm">{dayName}</span>
              </div>
              
              <div className={`space-y-2 transition-all ${state.enabled ? 'opacity-100' : 'opacity-50 pointer-events-none'}`}>
                <div>
                  <label className="mb-1 block text-[10px] uppercase tracking-wider text-muted">Start</label>
                  <input type="time" className="min-h-11 w-full rounded-md border border-surface-border p-1 text-xs" value={state.startTime.substring(0, 5)} onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, startTime: e.target.value + ':00'}}))} />
                </div>
                <div>
                  <label className="mb-1 block text-[10px] uppercase tracking-wider text-muted">End</label>
                  <input type="time" className="min-h-11 w-full rounded-md border border-surface-border p-1 text-xs" value={state.endTime.substring(0, 5)} onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, endTime: e.target.value + ':00'}}))} />
                </div>
                <div>
                  <label className="mb-1 block text-[10px] uppercase tracking-wider text-muted">Slots</label>
                  <input type="number" min="1" className="min-h-11 w-full rounded-md border border-surface-border p-1 text-xs" value={state.maxSlots} onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, maxSlots: parseInt(e.target.value) || 0}}))} />
                </div>
              </div>
            </div>
          )
        })}
      </div>
      <div className="flex justify-end gap-3">
        <Button variant="secondary" onClick={onCancel}>Cancel</Button>
        <Button onClick={handleSave} disabled={saving}>
          {saving ? "Saving..." : "Save Schedule"}
        </Button>
      </div>
    </div>
  );
}
