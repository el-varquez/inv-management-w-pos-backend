import { Modal } from '../../../components/Modal';
import { peso } from '../../../lib/format';
import type { TransactionResult } from '../../../types';

interface Props {
  result: TransactionResult;
  onClose: () => void;
}

/** Acknowledgment Receipt shown after a successful sale. */
export const ReceiptModal = ({ result, onClose }: Props) => (
  <Modal
    title="Sale complete"
    subtitle={`Acknowledgment Receipt · ${result.receiptNumber}`}
    onClose={onClose}
  >
    <div className="receipt">
      <div className="receipt-line">
        <span>Subtotal</span>
        <span className="tnum">{peso.format(result.subtotal)}</span>
      </div>
      <div className="receipt-line">
        <span>Discount</span>
        <span className="tnum">−{peso.format(result.discountAmount)}</span>
      </div>
      <div className="receipt-line receipt-grand">
        <span>Total</span>
        <span className="tnum">{peso.format(result.total)}</span>
      </div>
      <div className="receipt-line">
        <span>Tendered</span>
        <span className="tnum">{peso.format(result.amountTendered)}</span>
      </div>
      <div className="receipt-line receipt-change">
        <span>Change</span>
        <span className="tnum">{peso.format(result.change)}</span>
      </div>
    </div>

    <div className="modal-actions">
      <button type="button" className="btn btn-primary" onClick={onClose}>
        New sale
      </button>
    </div>
  </Modal>
);
