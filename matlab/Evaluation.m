function result = Evaluation(selection_index)

    label = load('label_clusterRumor.txt');
    root = '..\DataProcess\bin\Release\Feature\';
    fileNames = {'RatioOfSignal.txt', 'AvgCharLength_Signal.txt', 'AvgCharLength_All.txt', 'AvgCharLength_Ratio.txt', 'AvgWordLength_Signal.txt', 'AvgWordLength_All.txt', 'AvgWordLength_Ratio.txt', 'RtRatio_Signal.txt', 'RtRatio_All.txt', 'AvgUrlNum_Signal.txt', 'AvgUrlNum_All.txt', 'AvgHashtagNum_Signal.txt', 'AvgHashtagNum_All.txt', 'AvgMentionNum_Signal.txt', 'AvgMentionNum_All.txt', 'AvgRegisterTime_All.txt', 'AvgEclipseTime_All.txt', 'AvgFavouritesNum_All.txt', 'AvgFollwersNum_All.txt', 'AvgFriendsNum_All.txt', 'AvgReputation_All.txt', 'AvgTotalTweetNum_All.txt', 'AvgHasUrl_All.txt', 'AvgHasDescription_All.txt', 'AvgDescriptionCharLength_All.txt', 'AvgDescriptionWordLength_All.txt', 'AvgUtcOffset_All.txt', 'OpinionLeaderNum_All.txt', 'NormalUserNum_All.txt', 'OpinionLeaderRatio_All.txt', 'AvgQuestionMarkNum_All.txt', 'AvgExclamationMarkNum_All.txt', 'AvgUserRetweetNum_All.txt', 'AvgUserOriginalTweetNum_All.txt', 'AvgUserRetweetOriginalRatio_All.txt', 'AvgSentimentScore_All.txt', 'PositiveTweetRatio_All.txt', 'NegativeTweetRatio_All.txt', 'AvgPositiveWordNum_All.txt', 'AvgNegativeWordNum_All.txt', 'RetweetTreeRootNum_All.txt', 'RetweetTreeNonrootNum_All.txt', 'RetweetTreeMaxDepth_All.txt', 'RetweetTreeMaxBranchNum_All.txt', 'TotalTweetsCount_All.txt'};
    features = cell(1, length(fileNames));
    for i = 1:1:length(fileNames)
        features{i} = load([root, fileNames{i}]);
    end
    N = length(features);
    
    selection = zeros(1, N);
    selection(selection_index) = 1;
    result = EvaluateSelection(label, features, selection, 10);
    dlmwrite('evaluation_temp.txt', result, 'delimiter', ' ');
end