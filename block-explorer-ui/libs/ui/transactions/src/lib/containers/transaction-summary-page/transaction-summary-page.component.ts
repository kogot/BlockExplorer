import { Component, OnInit, OnDestroy } from '@angular/core';
import { Observable, ReplaySubject } from 'rxjs';
import { TransactionSummaryModel } from '@blockexplorer/shared/models';
import { ActivatedRoute } from '@angular/router';
import { TransactionsFacade } from '@blockexplorer/state/transactions-state';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'blockexplorer-transaction-summary-page',
  templateUrl: './transaction-summary-page.component.html',
  styleUrls: ['./transaction-summary-page.component.css']
})
export class TransactionSummaryPageComponent implements OnInit, OnDestroy {
  transactionsLoaded$: Observable<boolean>;
  destroyed$ = new ReplaySubject<any>();
  hash = '';
  transaction$: Observable<TransactionSummaryModel>;
  code = `
using OpenQA.Selenium;

namespace Stratis.Tests.UI.NetCore.Pages
{
    public class BasePage
    {
        public IWebDriver Driver { get; }

        public BasePage(IWebDriver driver)
        {
            this.Driver = driver;
        }

    }
}`;

  constructor(private route: ActivatedRoute, private transactionsFacade: TransactionsFacade) { }

  ngOnInit() {
    this.route.paramMap
        .pipe(takeUntil(this.destroyed$))
        .subscribe((paramMap: any) => {
          if (!!paramMap.params.hash) {
              this.hash = paramMap.params.hash;
              this.transactionsFacade.getTransaction(this.hash);
          }
        });
    this.loadTransactionDetails();
  }

  private loadTransactionDetails() {
    this.transactionsLoaded$ = this.transactionsFacade.loadedTransactions$;
    this.transaction$ = this.transactionsFacade.selectedTransaction$;
    this.transaction$.pipe(takeUntil(this.destroyed$))
        .subscribe(addressDetails => {
          console.log('Found transaction details', addressDetails);
        });
  }

  ngOnDestroy(): void {
    this.destroyed$.next(true);
    this.destroyed$.complete();
  }
}
