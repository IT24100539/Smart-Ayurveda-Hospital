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

  return (
    <div className="p-8 max-w-7xl mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold text-gray-800">Treatments</h1>
        <button 
          onClick={() => setDrawerOpen(true)}
          className="flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition"
        >
          <IconPlus /> New Treatment
        </button>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <div className="p-4 border-b border-gray-200 flex gap-4 bg-gray-50">
          <div className="relative flex-1">
            <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-gray-400">
              <IconSearch />
            </div>
            <input 
              type="text" 
              placeholder="Search treatments..." 
              className="w-full pl-10 pr-4 py-2 rounded-lg border border-gray-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>

        {loading ? (
          <div className="p-8 text-center text-gray-500">Loading treatments...</div>
        ) : (
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="bg-gray-100 text-gray-600 text-sm">
                <th className="py-3 px-4 font-semibold border-b">Name</th>
                <th className="py-3 px-4 font-semibold border-b">Category</th>
                <th className="py-3 px-4 font-semibold border-b">Status</th>
                <th className="py-3 px-4 font-semibold border-b text-center">Schedule (S M T W T F S)</th>
                <th className="py-3 px-4 font-semibold border-b text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {treatments.map(t => (
                <React.Fragment key={t.id}>
                  <tr 
                    className="border-b hover:bg-gray-50 cursor-pointer transition"
                    onClick={() => setExpandedRow(expandedRow === t.id ? null : t.id)}
                  >
                    <td className="py-4 px-4">
                      <div className="font-medium text-gray-900">{t.name}</div>
                      <div className="text-xs text-gray-500">{t.nameSinhala}</div>
                    </td>
                    <td className="py-4 px-4 text-gray-600">{TreatmentCategory[t.category as unknown as keyof typeof TreatmentCategory]}</td>
                    <td className="py-4 px-4">
                      <span className={`px-2 py-1 text-xs font-semibold rounded-full ${t.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                        {t.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="py-4 px-4">
                      <div className="flex justify-center gap-1">
                        {days.map((d, i) => (
                          <div 
                            key={i} 
                            className={`w-6 h-6 flex items-center justify-center rounded text-xs ${t.availableDays?.includes(i) ? 'bg-blue-100 text-blue-700 font-bold' : 'text-gray-300'}`}
                          >
                            {t.availableDays?.includes(i) ? '✓' : '—'}
                          </div>
                        ))}
                      </div>
                    </td>
                    <td className="py-4 px-4 text-right">
                      {t.isActive && (
                        <button 
                          onClick={(e) => handleDeactivate(t.id, e)}
                          className="text-red-500 hover:text-red-700 text-sm font-medium"
                        >
                          Deactivate
                        </button>
                      )}
                    </td>
                  </tr>
                  {expandedRow === t.id && (
                    <tr>
                      <td colSpan={5} className="bg-blue-50/50 p-0">
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
              {treatments.length === 0 && (
                <tr>
                  <td colSpan={5} className="p-8 text-center text-gray-500">No treatments found.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>

      {drawerOpen && (
        <div className="fixed inset-0 z-50 flex justify-end">
          <div className="absolute inset-0 bg-black/20" onClick={() => setDrawerOpen(false)} />
          <div className="relative w-96 bg-white shadow-2xl flex flex-col">
            <div className="flex justify-between items-center p-6 border-b">
              <h2 className="text-xl font-bold">New Treatment</h2>
              <button onClick={() => setDrawerOpen(false)} className="text-gray-500 hover:text-gray-800">
                <IconClose />
              </button>
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
        <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
        <input required type="text" className="w-full border rounded-lg p-2" value={formData.name} onChange={e => setFormData({...formData, name: e.target.value})} />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Name (Sinhala)</label>
        <input required type="text" className="w-full border rounded-lg p-2" value={formData.nameSinhala} onChange={e => setFormData({...formData, nameSinhala: e.target.value})} />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Category</label>
        <select className="w-full border rounded-lg p-2" value={formData.category} onChange={e => setFormData({...formData, category: Number(e.target.value) as TreatmentCategory})}>
          {Object.entries(TreatmentCategory).filter(([k]) => isNaN(Number(k))).map(([k, v]) => (
            <option key={String(v)} value={Number(v)}>{k}</option>
          ))}
        </select>
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea className="w-full border rounded-lg p-2" value={formData.description} onChange={e => setFormData({...formData, description: e.target.value})} />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Description (Sinhala)</label>
        <textarea className="w-full border rounded-lg p-2" value={formData.descriptionSinhala} onChange={e => setFormData({...formData, descriptionSinhala: e.target.value})} />
      </div>
      <div className="pt-4">
        <button type="submit" className="w-full bg-blue-600 text-white py-2 rounded-lg hover:bg-blue-700 font-medium">Create Treatment</button>
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

  if (!detail) return <div className="p-8 text-center text-sm text-gray-500">Loading schedule...</div>;

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
    <div className="p-6 bg-white border-y border-blue-100 shadow-inner" onClick={e => e.stopPropagation()}>
      <div className="flex justify-between items-center mb-4">
        <h3 className="font-bold text-gray-800">Edit Schedule for {detail.name}</h3>
      </div>
      <div className="grid grid-cols-7 gap-4 mb-6">
        {days.map((dayName, i) => {
          const state = editedDays[i];
          if (!state) return null;
          
          return (
            <div key={i} className={`p-3 rounded-xl border transition-colors ${state.enabled ? 'border-blue-300 bg-blue-50' : 'border-gray-200 bg-gray-50 opacity-70'}`}>
              <div className="flex items-center gap-2 mb-3">
                <input 
                  type="checkbox" 
                  checked={state.enabled}
                  onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, enabled: e.target.checked}}))}
                  className="w-4 h-4 text-blue-600 rounded"
                />
                <span className="font-semibold text-sm">{dayName}</span>
              </div>
              
              <div className={`space-y-2 transition-all ${state.enabled ? 'opacity-100' : 'opacity-50 pointer-events-none'}`}>
                <div>
                  <label className="block text-[10px] text-gray-500 uppercase tracking-wider mb-1">Start</label>
                  <input type="time" className="w-full text-xs p-1 border rounded" value={state.startTime.substring(0, 5)} onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, startTime: e.target.value + ':00'}}))} />
                </div>
                <div>
                  <label className="block text-[10px] text-gray-500 uppercase tracking-wider mb-1">End</label>
                  <input type="time" className="w-full text-xs p-1 border rounded" value={state.endTime.substring(0, 5)} onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, endTime: e.target.value + ':00'}}))} />
                </div>
                <div>
                  <label className="block text-[10px] text-gray-500 uppercase tracking-wider mb-1">Slots</label>
                  <input type="number" min="1" className="w-full text-xs p-1 border rounded" value={state.maxSlots} onChange={e => setEditedDays(prev => ({...prev, [i]: {...prev[i]!, maxSlots: parseInt(e.target.value) || 0}}))} />
                </div>
              </div>
            </div>
          )
        })}
      </div>
      <div className="flex justify-end gap-3">
        <button onClick={onCancel} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded-lg text-sm font-medium">Cancel</button>
        <button onClick={handleSave} disabled={saving} className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 disabled:opacity-50">
          {saving ? 'Saving...' : 'Save Schedule'}
        </button>
      </div>
    </div>
  );
}
