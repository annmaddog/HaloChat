import './CongTac.css';

interface CongTacProps {
  batTat: boolean;
  onDoi: (giaTri: boolean) => void;
  disabled?: boolean;
  nhan?: string;
}

export function CongTac({ batTat, onDoi, disabled, nhan }: CongTacProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={batTat}
      aria-label={nhan}
      className={`cong-tac${batTat ? ' cong-tac--bat' : ''}`}
      disabled={disabled}
      onClick={() => onDoi(!batTat)}
    >
      <span className="cong-tac__nut" />
    </button>
  );
}
