function PredictAndRank(selection_index, classifier)

    label = load('label_clusterRumor.txt');
    root = '..\DataProcess\bin\Release\Feature\';
    root_predict = '..\DataProcess\bin\Release\Feature_all\';
    fileNames = {'RatioOfSignal.txt', 'AvgCharLength_Signal.txt', 'AvgCharLength_All.txt', 'AvgCharLength_Ratio.txt', 'AvgWordLength_Signal.txt', 'AvgWordLength_All.txt', 'AvgWordLength_Ratio.txt', 'RtRatio_Signal.txt', 'RtRatio_All.txt', 'AvgUrlNum_Signal.txt', 'AvgUrlNum_All.txt', 'AvgHashtagNum_Signal.txt', 'AvgHashtagNum_All.txt', 'AvgMentionNum_Signal.txt', 'AvgMentionNum_All.txt', 'AvgRegisterTime_All.txt', 'AvgEclipseTime_All.txt', 'AvgFavouritesNum_All.txt', 'AvgFollwersNum_All.txt', 'AvgFriendsNum_All.txt', 'AvgReputation_All.txt', 'AvgTotalTweetNum_All.txt', 'AvgHasUrl_All.txt', 'AvgHasDescription_All.txt', 'AvgDescriptionCharLength_All.txt', 'AvgDescriptionWordLength_All.txt', 'AvgUtcOffset_All.txt', 'OpinionLeaderNum_All.txt', 'NormalUserNum_All.txt', 'OpinionLeaderRatio_All.txt', 'AvgQuestionMarkNum_All.txt', 'AvgExclamationMarkNum_All.txt', 'AvgUserRetweetNum_All.txt', 'AvgUserOriginalTweetNum_All.txt', 'AvgUserRetweetOriginalRatio_All.txt', 'AvgSentimentScore_All.txt', 'PositiveTweetRatio_All.txt', 'NegativeTweetRatio_All.txt', 'AvgPositiveWordNum_All.txt', 'AvgNegativeWordNum_All.txt', 'RetweetTreeRootNum_All.txt', 'RetweetTreeNonrootNum_All.txt', 'RetweetTreeMaxDepth_All.txt', 'RetweetTreeMaxBranchNum_All.txt', 'TotalTweetsCount_All.txt'};
    
    features = cell(1, length(fileNames));
    for i = 1:1:length(fileNames)
        features{i} = load([root, fileNames{i}]);
    end
    N = length(features);
    
    features_predict = cell(1, length(fileNames));
    for i = 1:1:length(fileNames)
        features_predict{i} = load([root_predict, fileNames{i}]);
    end
    m = length(features_predict{1});
    
    
    selection = zeros(1, N);
    selection(selection_index) = 1;
    
    feature = [];
    for i = 1:1:N
        if selection(i) == 0
            continue;
        end
        feature = [feature, features{i}];
    end
    
    feature_predict = [];
    for i = 1:1:N
        if selection(i) == 0
            continue;
        end
        feature_predict = [feature_predict, features_predict{i}];
    end
    
    filter_index = true(1, m);
    filter_feature = features_predict{N};
    filter_index(filter_feature >= 10) = false;
    if strcmp(classifier, 'DT')
        DT = fitctree(feature, label);
        [~, score] = predict(DT, feature_predict);
    else
        for i = 1:1:N
            features{i}(isnan(features{i})) = 0;
        end
        NB = fitNaiveBayes(feature, label);
        score = posterior(NB, feature_predict);
    end
    s = [score(:, 2) (0:m-1)'];
    s(filter_index, :) = [];
    rank = sortrows(s, [-1 2]);
    dlmwrite('predict.txt', rank);
end