import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TreatmentsView } from '../components/treatments/TreatmentsView';
import * as api from '../api/treatments';
import * as workflows from '../api/workflows';

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

vi.mock('../api/workflows', () => ({
  askTreatmentInfo: vi.fn()
}));

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
    vi.mocked(workflows.askTreatmentInfo).mockReset();
  });

  it('renders treatments list and schedule day-grid correctly', async () => {
    render(<TreatmentsView />);
    
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());
    
    const checkmarks = screen.getAllByText('✓');
    expect(checkmarks).toHaveLength(3);
    
    const dashes = screen.getAllByText('—');
    expect(dashes).toHaveLength(4);
  });

  it('opens inline schedule editor on row click and fetches details', async () => {
    render(<TreatmentsView />);
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());
    
    fireEvent.click(screen.getByText('Abhyanga'));
    
    await waitFor(() => {
      expect(screen.getByText('Edit Schedule for Abhyanga')).toBeInTheDocument();
      expect(api.getTreatmentDetails).toHaveBeenCalledWith('1');
    });

    const checkboxes = screen.getAllByRole('checkbox');
    expect(checkboxes).toHaveLength(7);
    expect(checkboxes[1]).toBeChecked();
    expect(checkboxes[0]).not.toBeChecked();
  });

  it('schedule toggle and save triggers the right API call shape', async () => {
    render(<TreatmentsView />);
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());
    
    fireEvent.click(screen.getByText('Abhyanga'));
    await waitFor(() => expect(screen.getByText('Edit Schedule for Abhyanga')).toBeInTheDocument());
    
    const checkboxes = screen.getAllByRole('checkbox');
    fireEvent.click(checkboxes[1]);
    fireEvent.click(checkboxes[0]);
    fireEvent.click(screen.getByText('Save Schedule'));
    
    await waitFor(() => {
      expect(api.deleteScheduleEntry).toHaveBeenCalledWith('1', 's1');
      expect(api.createScheduleEntry).toHaveBeenCalledWith('1', expect.objectContaining({
        dayOfWeek: 'Sunday',
        startTime: '09:00:00',
        endTime: '17:00:00',
        maxSlotsPerDay: 10
      }));
    });
  });

  it('asks the treatment-info agent and shows a grounded answer', async () => {
    vi.mocked(workflows.askTreatmentInfo).mockResolvedValue({
      answer: 'Panchakarma is listed on Monday, Wednesday, and Friday.',
      matchedTreatmentIds: ['aaaaaaaa-0000-0000-0000-000000000001'],
      refused: false,
      workflowId: 'wf-1'
    });

    render(<TreatmentsView />);
    await waitFor(() => expect(screen.getByText('Abhyanga')).toBeInTheDocument());

    fireEvent.change(screen.getByLabelText('Question'), {
      target: { value: 'When is Panchakarma available?' }
    });
    fireEvent.click(screen.getByRole('button', { name: 'Ask' }));

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Panchakarma is listed on Monday, Wednesday, and Friday.'
    );
    expect(workflows.askTreatmentInfo).toHaveBeenCalledWith('When is Panchakarma available?');
    expect(screen.getByText('1 matched')).toBeInTheDocument();
  });

  it('shows a refusal when the agent rejects medical advice', async () => {
    vi.mocked(workflows.askTreatmentInfo).mockResolvedValue({
      answer: 'Please consult hospital staff.',
      matchedTreatmentIds: [],
      refused: true,
      workflowId: 'wf-2'
    });

    render(<TreatmentsView />);
    await waitFor(() => expect(screen.getByText('Ask about treatments')).toBeInTheDocument());

    fireEvent.change(screen.getByLabelText('Question'), {
      target: { value: 'Should I take Nasya for sinusitis?' }
    });
    fireEvent.click(screen.getByRole('button', { name: 'Ask' }));

    expect(await screen.findByText('Medical advice refused')).toBeInTheDocument();
    expect(screen.getByRole('status')).toHaveTextContent('Please consult hospital staff.');
  });
});
