import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TreatmentsView } from '../components/treatments/TreatmentsView';
import * as api from '../api/treatments';

// Mock the API module
vi.mock('../api/treatments', () => ({
  getTreatments: vi.fn(),
  getTreatmentDetails: vi.fn(),
  createTreatment: vi.fn(),
  deactivateTreatment: vi.fn(),
  createScheduleEntry: vi.fn(),
  deleteScheduleEntry: vi.fn(),
  TreatmentCategory: {
    Consultation: 0,
    Panchakarma: 1
  }
}));

const mockTreatments = {
  items: [
    {
      id: '1',
      name: 'Abhyanga',
      nameSinhala: 'අභ්‍යංග',
      description: 'Full body massage',
      descriptionSinhala: 'සම්පූර්ණ ශරීර සම්බාහනය',
      category: 1,
      durationMinutes: 60,
      unitPrice: 2000,
      isActive: true,
      availableDays: [1, 3, 5] // Mon, Wed, Fri
    }
  ],
  totalCount: 1
};

const mockTreatmentDetail = {
  ...mockTreatments.items[0],
  schedule: [
    { id: 's1', treatmentId: '1', dayOfWeek: 1, startTime: '09:00:00', endTime: '12:00:00', maxSlotsPerDay: 5, isActive: true },
    { id: 's3', treatmentId: '1', dayOfWeek: 3, startTime: '13:00:00', endTime: '16:00:00', maxSlotsPerDay: 5, isActive: true },
    { id: 's5', treatmentId: '1', dayOfWeek: 5, startTime: '09:00:00', endTime: '12:00:00', maxSlotsPerDay: 5, isActive: true },
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
      
      // It should create a new entry for Sunday (dayOfWeek 0)
      expect(api.createScheduleEntry).toHaveBeenCalledWith('1', expect.objectContaining({
        dayOfWeek: 0,
        startTime: '09:00:00', // Default initial times we set in the component
        endTime: '17:00:00',
        maxSlotsPerDay: 10
      }));
    });
  });
});
