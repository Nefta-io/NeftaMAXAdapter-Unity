//
//  ALNeftaAdapter.h
//  NeftaMaxAdapter
//
//  Created by Tomaz Treven on 09/11/2023.
//

#ifndef ALNeftaAdapter_h
#define ALNeftaAdapter_h

#import "ALNeftaAd.h"

@interface ALNeftaMediationAdapter : ALMediationAdapter <MAAdViewAdapter, MAInterstitialAdapter, MARewardedAdapter>
typedef NS_ENUM(NSInteger, AdType) {
    AdTypeOther = 0,
    AdTypeBanner = 1,
    AdTypeInterstitial = 2,
    AdTypeRewarded = 3
};

+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner insight:(AdInsight * _Nullable)insight;
+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner;
+ (void)OnExternalMediationRequestWithBanner:(MAAdView * _Nonnull)banner customBidPrice:(double)customBidPrice;
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial insight:(AdInsight * _Nullable)insight;
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial;
+ (void)OnExternalMediationRequestWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial customBidPrice:(double)customBidPrice;
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded insight:(AdInsight * _Nullable)insight;
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded;
+ (void)OnExternalMediationRequestWithRewarded:(MARewardedAd * _Nonnull)rewarded customBidPrice:(double)customBidPrice;

+ (void)OnExternalMediationRequestLoadWithBanner:(MAAdView * _Nonnull)banner ad:(MAAd * _Nonnull)ad;
+ (void)OnExternalMediationRequestLoadWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial ad:(MAAd * _Nonnull)ad;
+ (void)OnExternalMediationRequestLoadWithRewarded:(MARewardedAd * _Nonnull)rewarded ad:(MAAd * _Nonnull)ad;

+ (void)OnExternalMediationRequestFailWithBanner:(MAAdView * _Nonnull)banner error:(MAError * _Nonnull)error;
+ (void)OnExternalMediationRequestFailWithInterstitial:(MAInterstitialAd * _Nonnull)interstitial error:(MAError * _Nonnull)error;
+ (void)OnExternalMediationRequestFailWithRewarded:(MARewardedAd * _Nonnull)rewarded error:(MAError * _Nonnull)error;

+ (void)OnExternalMediationImpression:(MAAd* _Nonnull)ad;
+ (void)OnExternalMediationClick:(MAAd* _Nonnull)ad;

@property ALNeftaAd * _Nullable ad;
@end

#endif /* ALNeftaAdapter_h */
