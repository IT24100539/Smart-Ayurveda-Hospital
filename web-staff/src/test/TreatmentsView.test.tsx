import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TreatmentsView } from '../components/treatments/TreatmentsView';
import * as api from '../api/treatments';

vi.mock('../api/treatments', async () => {
  const actual = await vi.importActual<typeof import('../api/treatments')>('../api/treatments');
  return {
    ...actual,
    getTreatments: vi.fn(),
    getTreatmentDetails: vi.fn(),
    createTreatment: vi.fn(),
    deactivateTreatment: vi.fn(),
    createScheduleEntry: vi.fn(),
    updateScheduleEntry: vi.fn(),
    deleteScheduleEntry: vi.fn()
  };
});

const mockTreatments = {
  items: [
    {
      id: '1',
      name: 'Abhyanga',
      nameSinhala: 'අභ්‍යංග',
      description: 'Full body massage',
      descriptionSinhala: 'සම්පූර්ණ ශරීර සම්බාහනය',
      category: 'Panchakarma',
      durationMinutes: 60,
      unitPrice: 2000,
      isActive: true,
      availableDays: ['Monday', 'Wednesday', 'Friday']
    }
  ],
  totalCount: 1
};

const mockTreatmentDetail = {
  ...mockTreatments.items[0],
  schedule: [
    { id: 's1', treatmentId: '1', dayOfWeek: 'Monday', startTime: '09:00:00', endTime: '12:00:00', maxSlotsPerDay: 5, isActive: true },
    { id: 's3', treatmentId: '1', dayOfWeek: 'Wednesday', startTime: '13:00:00', endTime: '16:00:00', maxSlotsPerDay: 5, isActive: true },
    { id: 's5', treatmentId: '1', dayOfWeek: 'Friday', startTime: '09:00:00', endTime: '12:00:00', maxSlotsPerDay: 5, isActive: true },
  ]
};

describe('TreatmentsView', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (api.getTreatments as any).mockResolvedValue(mockTreatments);
    (api.getTreatmentDetails as any).mockResolvedValue(mockTreatmentDetail);
  });

  it('renders treatments list and schedule day-grid correctly', async () => {
    render(<TreatmentsView />);
    
    // Wait for the table to load
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());
    
    // Check if the checkmarks are rendered for Mon (index 1), Wed (index 3), Fri (index 5)
    // The days array is ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
    const checkmarks = screen.getAllByText('✓');
    expect(checkmarks).toHaveLength(3);
    
    const dashes = screen.getAllByText('—');
    expect(dashes).toHaveLength(4); // 7 days total - 3 available = 4 unavailable
  });

  it('opens inline schedule editor on row click and fetches details', async () => {
    render(<TreatmentsView />);
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());
    
    fireEvent.click(screen.getByText('Abhyanga'));
    
    await waitFor(() => {
      expect(screen.getByText('Edit Schedule for Abhyanga')).toBeInTheDocument();
      expect(api.getTreatmentDetails).toHaveBeenCalledWith('1');
    });

    // Verify some day checkboxes are checked based on the mock data
    const checkboxes = screen.getAllByRole('checkbox');
    expect(checkboxes).toHaveLength(7); // 7 days
    
    // Day 1 (Mon) should be checked
    expect(checkboxes[1]).toBeChecked();
    // Day 0 (Sun) should not be checked
    expect(checkboxes[0]).not.toBeChecked();
  });

  it('schedule toggle and save triggers the right API call shape', async () => {
    render(<TreatmentsView />);
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());
    
    // Open editor
    fireEvent.click(screen.getByText('Abhyanga'));
    await waitFor(() => expect(screen.getByText('Edit Schedule for Abhyanga')).toBeInTheDocument());
    
    const checkboxes = screen.getAllByRole('checkbox');
    
    // Untoggle Mon (Day 1) - this should trigger a DELETE since it existed
    fireEvent.click(checkboxes[1]);
    
    // Toggle Sun (Day 0) - this should trigger a POST since it didn't exist
    fireEvent.click(checkboxes[0]);
    
    // Click save
    fireEvent.click(screen.getByText('Save Schedule'));
    
    await waitFor(() => {
      // It should delete the entry for Monday ('s1')
      expect(api.deleteScheduleEntry).toHaveBeenCalledWith('1', 's1');
      
      expect(api.createScheduleEntry).toHaveBeenCalledWith('1', expect.objectContaining({
        dayOfWeek: 'Sunday',
        startTime: '09:00:00',
        endTime: '17:00:00',
        maxSlotsPerDay: 10
      }));
    });
  });

  it('shows the catalogue error instead of writing it to the console', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    (api.getTreatments as any).mockRejectedValue(new Error('network down'));

    render(<TreatmentsView />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Unable to load treatments.');
    expect(consoleError).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it('soft-deactivates treatment when Deactivate button is clicked and confirmed', async () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);
    (api.getTreatments as any)
      .mockResolvedValueOnce(mockTreatments)
      .mockResolvedValue({
        items: [{ ...mockTreatments.items[0], isActive: false }],
        totalCount: 1
      });
    (api.deactivateTreatment as any).mockResolvedValue({
      ...mockTreatments.items[0],
      isActive: false
    });

    render(<TreatmentsView />);
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());

    expect(screen.getByText('Active')).toBeInTheDocument();
    const deactivateBtn = screen.getByRole('button', { name: 'Deactivate' });
    fireEvent.click(deactivateBtn);

    expect(confirmSpy).toHaveBeenCalledWith('Are you sure you want to deactivate this treatment?');
    expect(api.deactivateTreatment).toHaveBeenCalledWith('1');

    await waitFor(() => {
      expect(screen.getByText('Inactive')).toBeInTheDocument();
      expect(screen.getByText('Deactivated')).toBeInTheDocument();
    });

    // Clicking an inactive treatment displays the scheduling blocked notice
    fireEvent.click(screen.getByText('Abhyanga'));
    await waitFor(() => {
      expect(screen.getByText('Future scheduling is blocked')).toBeInTheDocument();
      expect(screen.getByText(/soft-deactivated/i)).toBeInTheDocument();
    });

    confirmSpy.mockRestore();
  });

  it('filters treatments by status (all, active, inactive)', async () => {
    (api.getTreatments as any).mockResolvedValue({
      items: [
        { ...mockTreatments.items[0], id: '1', name: 'Active Therapy', isActive: true },
        { ...mockTreatments.items[0], id: '2', name: 'Inactive Therapy', isActive: false }
      ],
      totalCount: 2
    });

    render(<TreatmentsView />);
    await waitFor(() => {
      expect(screen.getByText('Active Therapy')).toBeInTheDocument();
      expect(screen.getByText('Inactive Therapy')).toBeInTheDocument();
    });

    const statusSelect = screen.getByRole('combobox', { name: /filter by status/i });

    // Filter to active only
    fireEvent.change(statusSelect, { target: { value: 'active' } });
    expect(screen.getByText('Active Therapy')).toBeInTheDocument();
    expect(screen.queryByText('Inactive Therapy')).not.toBeInTheDocument();

    // Filter to inactive only
    fireEvent.change(statusSelect, { target: { value: 'inactive' } });
    expect(screen.queryByText('Active Therapy')).not.toBeInTheDocument();
    expect(screen.getByText('Inactive Therapy')).toBeInTheDocument();
  });
});
