% root = '..\DataProcess\bin\Release\';
% label = load([root, 'label_cluster.txt']);
% timeSim = load([root, 'clusterHashtagSimilarity.txt']);
% hashtagSim = load([root, 'clusterTfIdfSimilarity.txt']);
% nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
% 
% fid=fopen('tfIdf\AdditionSimple_KmeansNormal.txt','w');
% fprintf(fid, 'clusterTimeSimilarity.txt\r\n');
% fprintf(fid, 'clusterHashtagSimilarity.txt\r\n');
% fprintf(fid, 'clusterNameEntitySetSimilarity.txt\r\n');
% fprintf(fid, '\r\n');
% 
% nmi_max = 0;
% for i = 0.0:0.1:1.0
%     for j = 0.0:0.1:1.0-i
%         k = 1.0 - i - j;
%         A = i * timeSim + j * hashtagSim + k * nameEntitySim;
%         A = sparse(A);
%         for d = 0:1:0
% %             % region Spectral Clustering
% %             [labelE] = sc(A, 0, 686 + d);
% %             % endregion Spectral Clustering
%             
% %             % region Hierarchical Clustering
% %             A = 1 - A;
% %             N = size(A, 1);
% %             B = ones(1, N * (N - 1) / 2);
% %             index = 1;
% %             for ii = 1:1:N-1
% %                 for jj = ii+1:1:N
% %                     B(index) = A(jj, ii);
% %                     index = index + 1;
% %                 end
% %             end
% %             Z = linkage(B, 'ward');
% %             labelE = cluster(Z, 'maxclust', 686 + d);
% %                 % moce: single, complete, average, weighted, ward
% %             % endregion Hierarchical Clustering
%             
%             % region Kmeans Clustering
%             labelE = k_means(A, 'random', 686);
%             % endregion Kmeans Clustering
% 
% %             % region Kernel Kmeans Clustering
% %             [labelE] = knKmeans(A, 686, @knGauss);
% %             labelE = labelE';
% %             % endregion Kernel Kmeans Clustering
% 
%             nmi_value = nmi(label, labelE);
%             if nmi_value > nmi_max
%                nmi_max = nmi_value;
%                record_i = i;
%                record_j = j;
%                record_k = k;
%                record_K = 686 + d;
%             end
%             fprintf(fid, '%.1f %.1f %.1f %d: %f\r\n', i, j, k, 686 + d, nmi_value);
%         end
%     end
% end
% 
% fprintf(fid, '\r\n');
% fprintf(fid, 'Max NMI:\r\n');
% fprintf(fid, '%.1f %.1f %.1f %d: %f\r\n', record_i, record_j, record_k, record_K, nmi_max);
% fclose(fid);


root = '..\DataProcess\bin\Release\';
label = load([root, 'label_cluster.txt']);
timeSim = load([root, 'clusterTimeSimilarity.txt']);
hashtagSim = load([root, 'clusterHashtagSimilarity.txt']);
nameEntitySim = load([root, 'clusterNameEntitySetSimilarity.txt']);
jaccardSim = load([root, 'clusterWordJaccardSimilarity.txt']);
tfIdfSim = load([root, 'clusterTfIdfSimilarity.txt']);
mentionSim = load([root, 'clusterMentionSimilarity.txt']);

fid=fopen('combine\AdditionSimple_KmeansNormal.txt','w');
fprintf(fid, 'clusterTimeSimilarity.txt\r\n');
fprintf(fid, 'clusterHashtagSimilarity.txt\r\n');
fprintf(fid, 'clusterNameEntitySetSimilarity.txt\r\n');
fprintf(fid, 'clusterWordJaccardSimilarity.txt\r\n');
fprintf(fid, 'clusterTfIdfSimilarity.txt\r\n');
fprintf(fid, 'clusterMentionSimilarity.txt\r\n');
fprintf(fid, '\r\n');

nmi_max = 0;
for i = 0.0:0.1:1.0
    for j = 0.0:0.1:1.0-i
        for k = 0.0:0.1:1.0-i-j
            for l = 0.0:0.1:1.0-i-j-k
                for m = 0.0:0.1:1.0-i-j-k-l
                    n = 1.0-i-j-k-l-m;
                    A = i * timeSim + j * hashtagSim + k * nameEntitySim + l * jaccardSim + m * tfIdfSim + n * mentionSim;
                    A = sparse(A);
                    for d = 0:1:0
%                         % region Spectral Clustering
%                         [labelE] = sc(A, 0, 686 + d);
%                         % endregion Spectral Clustering
% 
%                         % region Hierarchical Clustering
%                         A = 1 - A;
%                         N = size(A, 1);
%                         B = ones(1, N * (N - 1) / 2);
%                         index = 1;
%                         for ii = 1:1:N-1
%                             for jj = ii+1:1:N
%                                 B(index) = A(jj, ii);
%                                 index = index + 1;
%                             end
%                         end
%                         Z = linkage(B, 'ward');
%                         labelE = cluster(Z, 'maxclust', 686 + d);
%                             % moce: single, complete, average, weighted, ward
%                         % endregion Hierarchical Clustering

                        % region Kmeans Clustering
                        labelE = k_means(A, 'random', 686);
                        % endregion Kmeans Clustering

%                         % region Kernel Kmeans Clustering
%                         [labelE] = knKmeans(A, 686, @knGauss);
%                         labelE = labelE';
%                         % endregion Kernel Kmeans Clustering

                        nmi_value = nmi(label, labelE);
                        if nmi_value > nmi_max
                           nmi_max = nmi_value;
                           record_i = i;
                           record_j = j;
                           record_k = k;
                           record_l = l;
                           record_m = m;
                           record_n = n;
                           record_K = 686 + d;
                        end
                        fprintf(fid, '%.1f %.1f %.1f %.1f %.1f %.1f %d: %f\r\n', i, j, k, l, m, n, 686 + d, nmi_value);
                    end
                end
            end
        end
    end
end

fprintf(fid, '\r\n');
fprintf(fid, 'Max NMI:\r\n');
fprintf(fid, '%.1f %.1f %.1f %.1f %.1f %.1f %d: %f\r\n', record_i, record_j, record_k, record_l, record_m, record_n, record_K, nmi_max);
fclose(fid);