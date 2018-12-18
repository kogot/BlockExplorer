import { Component, OnInit, Input } from '@angular/core';
import { BalanceResponseModel, BalanceSummaryModel } from '@blockexplorer/shared/models';

@Component({
  selector: 'blockexplorer-address-summary',
  templateUrl: './address-summary.component.html',
  styleUrls: ['./address-summary.component.css']
})
export class AddressSummaryComponent implements OnInit {

  @Input() hash = '';
  @Input() address: BalanceSummaryModel = null;

  constructor() { }

  ngOnInit() {
  }

  get confirmedBalance() {
    if (!this.address || !this.address.confirmed || !this.address.confirmed.amount || !this.address.confirmed.amount.satoshi)
      return 0;

    return this.address.confirmed.amount.satoshi;
  }

  get unconfirmedBalance() {
    if (!this.address || !this.address.unConfirmed || !this.address.unConfirmed.amount || !this.address.unConfirmed.amount.satoshi)
      return 0;

    return this.address.unConfirmed.amount.satoshi;
  }

  get spendableBalance() {
    if (!this.address || !this.address.spendable || !this.address.spendable.amount || !this.address.spendable.amount.satoshi)
      return 0;

    return this.address.spendable.amount.satoshi;
  }
}
