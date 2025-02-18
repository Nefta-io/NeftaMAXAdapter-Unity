//
//  ALNeftaInterstitial.m
//  MaxIntegration
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaInterstitial.h"

#import "ALNeftaMediationAdapter.h"

@implementation ALNeftaInterstitial

- (instancetype)initWithId:(NSString *)id listener:(id<MAInterstitialAdapterDelegate>)listener {
    self = [super init];
    if (self) {
        _interstitial = [[NInterstitial alloc] initWithId: id];
        _interstitial._listener = self;
        _listener = listener;
    }
    return self;
}

- (void) SetCustomParameterWithProvider:(NSString *)provider value: (NSString *)value {
    [_interstitial SetCustomParameterWithProvider: provider value: value];
}
- (void) Load {
    [_interstitial Load];
}
- (int) CanShow {
    return (int)[_interstitial CanShow];
}
- (void) Show {
    [_interstitial Show];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    [_listener didFailToLoadInterstitialAdWithError: MAAdapterError.unspecified];
}
- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    [_listener didLoadInterstitialAd];
}
- (void)OnShowFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {

}
- (void)OnShowWithAd:(NAd * _Nonnull)ad {
    [_listener didDisplayInterstitialAd];
}
- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_listener didClickInterstitialAd];
}
- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
    [_listener didHideInterstitialAd];
}
@end
